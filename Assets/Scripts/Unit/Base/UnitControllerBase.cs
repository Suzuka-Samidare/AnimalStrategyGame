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
}
