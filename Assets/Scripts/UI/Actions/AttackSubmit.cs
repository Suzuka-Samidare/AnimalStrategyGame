using UnityEngine;
using TimelineCommand = TimelineManager.TimelineCommand;

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
        TimelineCommand command = _timelineManager.CreatePlayerCommand();
        _timelineManager.RegisterCommand(command);
    }
}
