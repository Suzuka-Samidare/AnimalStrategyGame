using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UnitControllerBase : MonoBehaviour
{
    [Header("Refs")]
    protected UnitStatsBase stats;
    protected UnitAnimationBase unitAnimation;

    public virtual void Initialize(UnitBase unitBase)
    {
        stats = unitBase.Stats;
        unitAnimation = unitBase.Animation;
    }

    /// <summary>
    /// 気絶処理
    /// </summary>
    public async UniTask OnFaint(Tile tile)
    {
        if (unitAnimation)
        {
            await unitAnimation.PlayOnceAsync(AnimationName.Death);
        }
        UnitSpawnManager.Instance.DespawnUnit(tile);
    }
}
