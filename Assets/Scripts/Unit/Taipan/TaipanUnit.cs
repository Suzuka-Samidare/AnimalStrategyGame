using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TaipanStats))]
[RequireComponent(typeof(TaipanController))]
[RequireComponent(typeof(TaipanAnimation))]
[RequireComponent(typeof(LinearMover))]
public class TaipanUnit : DefencerUnitBase<TaipanStats, TaipanController>
{
    public LinearMover LinearMover { get; private set; }
    
    protected override void Awake()
    {
        base.Awake();

        LinearMover = GetComponent<LinearMover>();
    }
}
