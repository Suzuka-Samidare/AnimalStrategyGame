using UnityEngine;

public class HeadquarterUnit : UnitBase
{
    [SerializeField] private HeadquarterStats _stats;
    // [SerializeField] private HeadquarterController _controller;
    // [SerializeField] private HeadquarterAnimation _animation;

    // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    public override UnitStatsBase Stats => _stats;
    public override UnitControllerBase Controller => null;
    public override UnitAnimationBase Animation => null;
}
