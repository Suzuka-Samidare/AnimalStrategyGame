using UnityEngine;

public abstract class UnitControllerBase : MonoBehaviour
{
    [Header("Refs")]
    protected UnitStatsBase stats;
    protected UnitAnimationBase unitAnimation;

    [Header("位置情報")]
    [Tooltip("可視状態時の座標設定")] private Vector3 _visiblePos;
    [Tooltip("不可視状態時の座標設定")] private Vector3 _invisiblePos;

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
        Debug.Log(stats.IsVisible);

        if (stats.Owner == Owner.Player || stats.IsVisible)
        {
            transform.position = _visiblePos;
        }
        else
        {
            transform.position = _invisiblePos;
        }
    }
}
