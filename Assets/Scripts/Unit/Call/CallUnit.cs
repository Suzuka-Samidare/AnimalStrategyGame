using UnityEngine;

public class CallUnit : UnitBase
{
    [SerializeField] private CallStats _stats;
    // [SerializeField] private CallController _controller;
    // [SerializeField] private CallAnimation _animation;

    // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    public override UnitStatsBase Stats => _stats;
    public override UnitControllerBase Controller => null;
    public override UnitAnimationBase Animation => null;
}
