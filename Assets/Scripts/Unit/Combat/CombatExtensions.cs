using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 交戦（直接戦闘）機能を持つユニットであることを示すインターフェース
/// </summary>
public interface ICombatable
{
    Transform transform { get; }
    UnitStatsBase Stats { get; }
    LinearMover LinearMover { get; }
    UnitAnimationBase UnitAnimation { get; }
    Timer RecastTimer { get; }

    bool IsAttacking { get; set; }
    bool IsAttackReady { get; set; }
}

public static class CombatExtensions
{
    /// <summary>
    /// 交戦処理の開始｛条件：トークンがキャンセルされるまで）
    /// </summary>
    public static async UniTask RunCombatLoopAsync(this ICombatable self, Tile myTile, List<Tile> enemyUnitTiles, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (self.Stats != null && self.Stats.IsFaint)
                {
                    Debug.Log($"[{self.Stats.name}] 気絶したため個別の交戦ループを終了します。");
                    await myTile.OnFaintUnit();
                    return;
                }
                await self.ExecuteCombatAsync(enemyUnitTiles);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // CancellationTokenによるループ中断で処理できなかった気絶処理を必要に応じて実行
            if (self.Stats != null && self.Stats.IsFaint)
            {
                await myTile.OnFaintUnit();
            }
            throw;
        }
    }

    /// <summary>
    /// 直接交戦処理の実行（CanDirectCombatが有効なユニットのみ）
    /// </summary>
    private static async UniTask ExecuteCombatAsync(this ICombatable self, List<Tile> enemyUnitTiles)
    { 
        if (!self.Stats.combatProfile.CanDirectCombat) return;
        // {
        //     throw new InvalidOperationException($"[ExecuteCombat] {self.Stats.name} is not allowed to engage in direct combat.");
        // }

        // リキャストタイマーを経過時間分進める
        self.RecastTimer.UpdateTick(Time.deltaTime);

        // 一番近い敵ユニットの取得
        UnitBase nearestEnemy = self.GetNearestEnemyUnit(enemyUnitTiles);

        // 気絶していない敵ユニットがいなかった場合は処理終了
        if (nearestEnemy == null) return;

        // 処理に必要な各種数値を用意
        Vector3 nearestEnemyPosition = nearestEnemy.transform.position;
        Vector3 offset = nearestEnemyPosition - self.transform.position;
        // 一番近い敵ユニットがすぐ目の前にいるか
        bool isEnemyInFront;
        // 攻撃範囲に入っているか計算結果を取得
        bool isInCombatRange = self.IsTargetInCombatRange(nearestEnemyPosition, out isEnemyInFront);
        // 一番近い敵ユニットに向かって回転、敵ユニットに向いている状態かを取得
        bool isFacingNearestEnemy = self.LinearMover.RotateTowards(offset);

        if (!self.IsAttacking && isInCombatRange && isFacingNearestEnemy && self.IsAttackReady)
        {
            self.IsAttacking = true;

            float power = GetProfilePower(self.Stats);
            // 攻撃
            await self.UnitAnimation.PlayOnceAsync(AnimationName.Attack);
            nearestEnemy.Stats.ApplyDamage(power);
            ParticleManager.Instance.PerformFireExplosionAsync(nearestEnemyPosition + Vector3.up, Quaternion.identity).Forget();
            FloatingTextPresenter.Instance.SpawnDamageAsync(nearestEnemyPosition, power).Forget();
            // リキャスト開始
            self.RecastTimer.Reset();
            self.IsAttackReady = false;
            self.RecastTimer.Start(self.Stats.combatProfile.RecastTime);
            self.IsAttacking = false;
        }
        else
        {
            if (isEnemyInFront || !isFacingNearestEnemy || self.Stats is DefencerStatsBase _) return;
            self.LinearMover.MoveInDirection(offset);
        }
    }

    /// <summary>
    /// 継承されたStatsのパワー値を取得する（ない場合は0）
    /// </summary>
    private static float GetProfilePower(UnitStatsBase stats)
    {
        if (stats is AttackerStatsBase attackerStats) return attackerStats.attackProfile.power;
        if (stats is DefencerStatsBase defencerStats) return defencerStats.defenceProfile.power;
        return 0f;
    }

    /// <summary>
    /// リストから一番近い敵ユニットを取得する（CanDirectCombatが有効なユニットのみ）
    /// </summary>
    private static UnitBase GetNearestEnemyUnit(this ICombatable self, List<Tile> enemyUnitTiles)
    {
        if (!self.Stats.combatProfile.CanDirectCombat)
        {
            throw new InvalidOperationException($"[ExecuteCombat] {self.Stats.name} is not allowed to engage in direct combat.");
        }
        
        UnitBase nearestUnit = null;
        float minSqrDistance = float.MaxValue;
        // リストにあるユニットとの距離を計算
        foreach (var tile in enemyUnitTiles)
        {
            if (tile.Unit is null || tile.Unit.Stats.IsFaint) continue;

            // ベクトルの差分から 2乗距離 を計算（平方根計算を排除して高速化）
            Vector3 offset = tile.Unit.transform.position - self.transform.position;
            float sqrDistance = offset.sqrMagnitude;
            // 最小距離の更新
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearestUnit = tile.Unit;
            }
        }
        // for (int i = 0; i < enemyUnitTiles.Count; i++)
        // {
        //     UnitBase enemyUnit = enemyUnitTiles[i].Unit;
        //     // 既に気絶しているユニットだった場合は次の処理へ
        //     if (enemyUnit.Stats.IsFaint) continue;
        //     // ベクトルの差分から 2乗距離 を計算（平方根計算を排除して高速化）
        //     Vector3 offset = enemyUnit.transform.position - self.transform.position;
        //     float sqrDistance = offset.sqrMagnitude;
        //     // 最小距離の更新
        //     if (sqrDistance < minSqrDistance)
        //     {
        //         minSqrDistance = sqrDistance;
        //         nearestUnit = enemyUnit;
        //     }
        // }
        return nearestUnit;
    }

    /// <summary>
    /// 距離を監視して攻撃条件を満たしているか判定するメソッド（CanDirectCombatが有効なユニットのみ）
    /// </summary>
    private static bool IsTargetInCombatRange(this ICombatable self, Vector3 targetPosition, out bool isEnemyInFront)
    {
        if (!self.Stats.combatProfile.CanDirectCombat)
        {
            throw new InvalidOperationException($"[ExecuteCombat] {self.Stats.name} is not allowed to engage in direct combat.");
        }

        Vector3 myGlobalPosition = self.transform.position;
        float engagementRange = self.Stats.combatProfile.EngagementRange;
        float sqrEngagementRange = engagementRange * engagementRange;
        Vector3 offset = targetPosition - myGlobalPosition;
        float sqrOffset = offset.sqrMagnitude;
        isEnemyInFront = sqrOffset <= 1f;

        return sqrOffset <= sqrEngagementRange;
    }
}
