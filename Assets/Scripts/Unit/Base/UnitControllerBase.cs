using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UnitControllerBase : MonoBehaviour
{
    [Header("Refs")]
    protected UnitStatsBase stats;
    protected UnitAnimationBase unitAnimation;

    [Header("位置情報")]
    [Tooltip("可視状態時の座標設定"), SerializeField] private Vector3 _visiblePos;
    [Tooltip("不可視状態時の座標設定"), SerializeField] private Vector3 _invisiblePos;

    public virtual void Initialize(UnitBase unitBase)
    {
        stats = unitBase.Stats;
        unitAnimation = unitBase.Animation;
        _visiblePos = transform.position;
        _invisiblePos = _visiblePos + new Vector3(0f, -9999f, 0f);
    }

    /// <summary>
    /// ユニットを可視フラグに応じて不可視状態にする
    /// </summary>
    public void UpdateVisibility()
    {
        if (stats.Owner == Owner.Player || stats.IsVisible)
        {
            transform.position = _visiblePos;
        }
        else
        {
            transform.position = _invisiblePos;
        }
    }

    /// <summary>
    /// 直接交戦処理の開始｛条件：トークンがキャンセルされるまで）
    /// </summary>
    public async UniTask RunDirectCombatLoopAsync(Tile myTile, List<Tile> enemyUnitTiles, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 気絶フラグが立っている場合
                if (stats != null && stats.IsFaint)
                {
                    Debug.Log($"[{stats.name}] 気絶したため個別の交戦ループを終了します。");
                    await myTile.OnFaintUnit();
                    return;
                }
                // 攻撃可能なユニットの場合
                if (this is ICombatable combatable)
                {
                    await combatable.ExecuteCombatAsync(enemyUnitTiles);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // CancellationTokenによるループ中断で処理できなかった気絶処理を必要に応じて実行
            if (stats != null && stats.IsFaint)
            {
                await myTile.OnFaintUnit();
            }
            throw;
        }
    }
}
