using UnityEngine;
using TimelineCommand = TimelineManager.TimelineCommand;

public class AttackSubmit : MonoBehaviour, IButtonAction
{
    [Header("Refs")]
    private TimelineManager _timelineManager;
    private PlayerManager _playerManager;

    private void Start()
    {
        _timelineManager = TimelineManager.Instance;
        _playerManager = PlayerManager.Instance;
    }

    public void Execute()
    {
        TimelineCommand command = _timelineManager.CreatePlayerCommand();
        _timelineManager.RegisterPlayerCommand(command);
        _playerManager.UseEnergy(command.AttackerUnit.Stats.attackProfile.energy);
        
        if (command.AttackerTile.Unit is AttackerUnitBase attackerUnit)
        {
            attackerUnit.EnableAttackSchedule();
        }
    }
}
