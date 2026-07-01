using UnityEngine;

public class SquidUnit : UnitBase
{
    [SerializeField] private SquidStats _stats;
    [SerializeField] private SquidController _controller;
    [SerializeField] private SquidAnimation _animation;

    // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    public override UnitStatsBase Stats => _stats;
    public override UnitControllerBase Controller => _controller;
    public override UnitAnimationBase Animation => _animation;
}
