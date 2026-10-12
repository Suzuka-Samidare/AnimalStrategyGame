using System;
using UnityEngine;

public class ColobusController : AttackerControllerBase, ICombatable
{
    [SerializeField] private Timer _recastTimer = new Timer();
    [SerializeField] private LinearMover _linearMover;

    // ICombatable用
    public UnitAnimationBase UnitAnimation => unitAnimation;
    public UnitStatsBase Stats => attackerStats;
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

    // private float _attackDistance => attackProfile.attackDistance;
    // private LinearMover _linearMover;

    // private void Awake()
    // {
    //     _linearMover = GetComponent<LinearMover>();
    // }

    // private void Update()
    // {
    //     if (attackerStats.IsEngaged)
    //     {
    //         // if (IsTargetInAttackRange())
    //         // {
                
    //         // }
    //     }
    // }

    // // 距離を監視して攻撃条件を満たしているか判定するメソッド
    // public bool IsTargetInAttackRange(Vector3 targetPosition)
    // {
    //     Vector3 myGlobalPosition = transform.position;
    //     Vector3 offset = targetPosition - myGlobalPosition;
    //     float sqrRange = _attackDistance * _attackDistance;

    //     // 平方根計算（Mathf.Sqrt）を避けるため sqrMagnitude を使用（最速）
    //     return offset.sqrMagnitude <= sqrRange;
    // }
}
