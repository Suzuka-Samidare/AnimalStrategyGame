using UnityEngine;

[RequireComponent(typeof(CallStats))]
[RequireComponent(typeof(CallController))]
public class CallUnit : UnitBase<CallStats, CallController>
{
    // [SerializeField] private CallStats _stats;
    // [SerializeField] private CallController _controller;
    // [SerializeField] private CallAnimation _animation;

    // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    // public override UnitStatsBase Stats => _stats;
    // public override UnitControllerBase Controller => _controller;
    // public override UnitAnimationBase Animation => null;
}
