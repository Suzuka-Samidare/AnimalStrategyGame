using UnityEngine;

[RequireComponent(typeof(ColobusStats))]
[RequireComponent(typeof(ColobusController))]
[RequireComponent(typeof(ColobusAnimation))]
public class ColobusUnit : AttackerUnitBase<ColobusStats, ColobusController>
{
    // [SerializeField] private ColobusStats _stats;
    // [SerializeField] private ColobusController _controller;
    // [SerializeField] private ColobusAnimation _animation;

    // // 抽象プロパティを自分のコンポーネントで上書きして外部に公開
    // public override UnitStatsBase Stats => _stats;
    // public override UnitControllerBase Controller => _controller;
    // public override UnitAnimationBase Animation => _animation;

    [ContextMenu("Visible")]
    public void TestVisible()
    {
        this.SetVisible(true);
    }

    [ContextMenu("Invisible")]
    public void TestInvisible()
    {
        this.SetVisible(false);
    }
}
