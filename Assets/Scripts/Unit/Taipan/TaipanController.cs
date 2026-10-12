using System;

public class TaipanController : DefencerControllerBase, ICombatable
{
    private Timer _recastTimer = new Timer();
    private LinearMover _linearMover;

    // ICombatable用
    public UnitAnimationBase UnitAnimation => unitAnimation;
    public UnitStatsBase Stats => defencerStats;
    public LinearMover LinearMover => _linearMover;
    public Timer RecastTimer => _recastTimer;
    public bool IsAttackReady { get; set; } = true;
    public bool IsAttacking { get; set; } = false;

    private void Awake()
    {
        Action onAttackReady = () => IsAttackReady = true;
        _recastTimer.OnTimerComplete += onAttackReady;
    }

    private void Start()
    {
        _linearMover = GetComponent<LinearMover>();
    }
}
