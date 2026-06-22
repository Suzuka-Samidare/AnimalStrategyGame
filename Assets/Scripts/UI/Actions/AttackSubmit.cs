using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AttackSubmit : MonoBehaviour, IButtonAction
{
    [Header("Refs")]
    private TimelineManager _timelineManager;

    private void Start()
    {
        _timelineManager = TimelineManager.Instance;
    }

    public void Execute()
    {
        _timelineManager.RegisterCommand();
    }
}
