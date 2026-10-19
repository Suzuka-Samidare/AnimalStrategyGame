using UnityEngine;

public class AttackSubmitCondition : MonoBehaviour, IButtonCondition
{
    private TileManager _tileManager;
    private PlayerManager _playerManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
        _playerManager = PlayerManager.Instance;
    }

    public bool CanInteract()
    {
         if (_tileManager.targetTile == null
            || _tileManager.selectedTile == null
            || _tileManager.selectedTile.Unit is not AttackerUnitBase attackerUnit
            || _playerManager.energy < attackerUnit.Stats.attackProfile.energy) return false;

         return true;
    }
}
