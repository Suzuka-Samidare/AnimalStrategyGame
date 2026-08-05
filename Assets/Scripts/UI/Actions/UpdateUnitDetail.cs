using UnityEngine;

public class UpdateUnitDetail : MonoBehaviour, IButtonAction
{
    private enum TargetTileType
    {
        PlayerMapSelected,
        EnemyMapTarget
    }
    [SerializeField, Tooltip("確認するタイル")]
    private TargetTileType _targetTileType;

    [Header("Refs")]
    private TileManager _tileManager;
    private UnitDetailController _unitDetailController;

    private void Start()
    {
        _tileManager = TileManager.Instance;
        _unitDetailController = UnitDetailController.Instance;
    }

    public void Execute()
    {
        if (_targetTileType == TargetTileType.PlayerMapSelected &&
            _tileManager.selectedTile.Unit != null)
        {
            _unitDetailController.Open(_tileManager.selectedTile.Unit.Stats);
        }
        else if (_targetTileType == TargetTileType.EnemyMapTarget &&
            _tileManager.targetTile.Unit != null)
        {
            _unitDetailController.Open(_tileManager.targetTile.Unit.Stats);
        }
        else
        {
            _unitDetailController.Close();
        }
    }
}