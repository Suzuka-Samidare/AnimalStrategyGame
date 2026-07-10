using UnityEngine;
using MapId = MapManager.MapId;


public class AttackPrepareCondition : MonoBehaviour, IButtonCondition
{
    private TileManager _tileManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
    }

    public bool CanInteract()
    {
        if (_tileManager.selectedTile == null) return false;

        if (_tileManager.selectedTile.Unit is not AttackerUnitBase _) return false;

        return true;
    }
}
