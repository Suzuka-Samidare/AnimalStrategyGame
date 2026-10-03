using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackerControllerBase : UnitControllerBase, ICombatable
{
    protected AttackerStatsBase attackerStats => stats as AttackerStatsBase;
    protected AttackProfile attackProfile => attackerStats.attackProfile;
    [SerializeField] private Timer _recastTimer = new Timer();
    [SerializeField] private LinearMover _linearMover;

    // ICombatable用
    public UnitAnimationBase UnitAnimation => unitAnimation;
    public UnitStatsBase Stats => attackerStats;
    public LinearMover LinearMover => _linearMover;
    public Timer RecastTimer => _recastTimer;
    public bool IsAttackReady { get; set; } = true;
    public bool IsAttacking { get; set; } = false;

    private void Awake()
    {
        Action onAttackReady = () => IsAttackReady = true;
        _recastTimer.OnTimerComplete += onAttackReady;
    }

    private void Start()
    {
        if (attackerStats.combatProfile.CanDirectCombat)
        {
            _linearMover = GetComponent<LinearMover>();
        }
    }

    /// <summary>
    /// 直接交戦処理の実行（CanDirectCombatが有効なユニットのみ）
    /// </summary>
    public List<Vector2Int> GetTargetTilePositions(Vector2Int targetPos)
    {
        List<Vector2Int> tilePositions = new List<Vector2Int>();
        // 単体攻撃ならそのマスだけ
        if (attackProfile.style == AttackType.Single)
        {
            tilePositions.Add(targetPos);
            return tilePositions;
        }
        // 範囲攻撃ならTileRangeUtilを使ってリストを埋める
        switch (attackProfile.style)
        {
            case AttackType.Square:
                TileRangeUtil.ForEachSquareRange(targetPos, attackProfile.gridRange.max, 
                    (pos) => tilePositions.Add(pos));
                break;
            case AttackType.Manhattan:
                TileRangeUtil.ForEachManhattanRange(targetPos, attackProfile.gridRange.max,
                    (pos) => tilePositions.Add(pos));
                break;
            // DEBUG =======================================================
            // case AttackType.Test:
            //     TileRangeUtil.GetForwardVerticalRange(
            //         targetPos,
            //         MapManager.Instance.mapHeight - 1 - targetPos.y,
            //         1,
            //         (pos) => tilePositions.Add(pos)
            //     );
            //     break;
            // DEBUG =======================================================
        }
        return tilePositions;
    }

    // /// <summary>
    // /// 直接交戦処理の実行（CanDirectCombatが有効なユニットのみ）
    // /// </summary>
    // public async UniTask ExecuteCombat(List<UnitBase> enemyUnits)
    // {
    //     if (!attackerStats.combatProfile.CanDirectCombat)
    //     {
    //         throw new InvalidOperationException($"[ExecuteCombat] {attackerStats.name} is not allowed to engage in direct combat.");
    //     }

    //     // リキャストタイマーを経過時間分進める
    //     _recastTimer.UpdateTick(Time.deltaTime);

    //     // 一番近い敵ユニットの取得
    //     UnitBase nearestEnemy = GetNearestEnemyUnit(enemyUnits);

    //     // 気絶していない敵ユニットがいなかった場合は処理終了
    //     if (nearestEnemy == null) return;

    //     // 処理に必要な各種数値を用意
    //     Vector3 nearestEnemyPosition = nearestEnemy.transform.position;
    //     Vector3 offset = nearestEnemyPosition - transform.position;
    //     // 一番近い敵ユニットがすぐ目の前にいるか
    //     bool isEnemyInFront;
    //     // 攻撃範囲に入っているか計算結果を取得
    //     bool isInCombatRange = IsTargetInCombatRange(nearestEnemyPosition, out isEnemyInFront);
    //     // 一番近い敵ユニットに向かって回転、敵ユニットに向いている状態かを取得
    //     bool isFacingNearestEnemy = _linearMover.RotateTowards(offset);

    //     // Debug.Log($"_isAttacking: {_isAttacking} \nisInCombatRange: {isInCombatRange} \nisFacingNearestEnemy: {isFacingNearestEnemy} \n_isAttackReady: {_isAttackReady}");

    //     if (!_isAttacking && isInCombatRange && isFacingNearestEnemy && _isAttackReady)
    //     {
    //         _isAttacking = true;
    //         // 攻撃
    //         await unitAnimation.PlayOnceAsync(AnimationName.Attack);
    //         nearestEnemy.Stats.ApplyDamage(attackerStats.attackProfile.power);
    //         ParticleManager.Instance.PerformFireExplosionAsync(nearestEnemyPosition + Vector3.up, Quaternion.identity).Forget();
    //         FloatingTextPresenter.Instance.SpawnDamageAsync(nearestEnemyPosition, attackerStats.attackProfile.power).Forget();
    //         // リキャスト開始
    //         _recastTimer.Reset();
    //         _isAttackReady = false;
    //         _recastTimer.Start(attackerStats.combatProfile.RecastTime);
    //         _isAttacking = false;
    //     }
    //     else
    //     {
    //         if (isEnemyInFront || !isFacingNearestEnemy) return;
    //         _linearMover.MoveInDirection(offset);
    //     }
    // }

    // /// <summary>
    // /// リストから一番近い敵ユニットを取得する（CanDirectCombatが有効なユニットのみ）
    // /// </summary>
    // private UnitBase GetNearestEnemyUnit(List<UnitBase> enemyUnits)
    // {
    //     if (!attackerStats.combatProfile.CanDirectCombat)
    //     {
    //         throw new InvalidOperationException($"[ExecuteCombat] {attackerStats.name} is not allowed to engage in direct combat.");
    //     }
        
    //     UnitBase nearestUnit = null;
    //     float minSqrDistance = float.MaxValue;
    //     // リストにあるユニットとの距離を計算
    //     for (int i = 0; i < enemyUnits.Count; i++)
    //     {
    //         UnitBase enemyUnit = enemyUnits[i];
    //         // 既に気絶しているユニットだった場合は次の処理へ
    //         if (enemyUnit.Stats.IsFaint) continue;
    //         // ベクトルの差分から 2乗距離 を計算（平方根計算を排除して高速化）
    //         Vector3 offset = enemyUnit.transform.position - transform.position;
    //         float sqrDistance = offset.sqrMagnitude;
    //         // 最小距離の更新
    //         if (sqrDistance < minSqrDistance)
    //         {
    //             minSqrDistance = sqrDistance;
    //             nearestUnit = enemyUnit;
    //         }
    //     }
    //     return nearestUnit;
    // }


    // /// <summary>
    // /// 距離を監視して攻撃条件を満たしているか判定するメソッド（CanDirectCombatが有効なユニットのみ）
    // /// </summary>
    // private bool IsTargetInCombatRange(Vector3 targetPosition, out bool isEnemyInFront)
    // {
    //     if (!attackerStats.combatProfile.CanDirectCombat)
    //     {
    //         throw new InvalidOperationException($"[ExecuteCombat] {attackerStats.name} is not allowed to engage in direct combat.");
    //     }

    //     Vector3 myGlobalPosition = transform.position;
    //     float engagementRange = attackerStats.combatProfile.EngagementRange;
    //     float sqrEngagementRange = engagementRange * engagementRange;
    //     Vector3 offset = targetPosition - myGlobalPosition;
    //     float sqrOffset = offset.sqrMagnitude;
    //     isEnemyInFront = sqrOffset <= 1f;

    //     return sqrOffset <= sqrEngagementRange;
    // }
}
