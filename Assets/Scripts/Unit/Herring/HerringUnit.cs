using UnityEngine;

[RequireComponent(typeof(HerringStats))]
[RequireComponent(typeof(HerringStats))]
[RequireComponent(typeof(HerringStats))]
public class HerringUnit : DefencerUnitBase<HerringStats, HerringController>
{
    // [SerializeField] private HerringStats _stats;
    // [SerializeField] private HerringController _controller;
    // [SerializeField] private HerringAnimation _animation;

    // // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    // public override UnitStatsBase Stats => _stats;
    // public override UnitControllerBase Controller => _controller;
    // public override UnitAnimationBase Animation => _animation;
}
