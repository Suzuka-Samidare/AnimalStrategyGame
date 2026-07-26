using System;
using UnityEngine;

public class UnitCallerCondition : MonoBehaviour, IButtonCondition
{
    [Tooltip("ユニットデータ")]
    private UnitData _unitData;

    [Header("Refs")]
    private DialogController _dialogController;
    private TileManager _tileManager;
    private PlayerManager _playerManager;

    private void Start()
    {
        _dialogController = DialogController.Instance;
        _tileManager = TileManager.Instance;
        _playerManager = PlayerManager.Instance;
    }

    public void Initialize(UnitData unitData)
    {
        if (_unitData != null) return;
        _unitData = unitData;
    }

    public bool CanInteract()
    {
        if (_dialogController.IsOpen) return false;

        if ( _tileManager.selectedTile == null) return false;

        if (_tileManager.selectedTile.Unit) return false;

        if (_playerManager.animalPoint < _unitData.cost) return false;

        return true;
    }
}
