using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCallerCondition : MonoBehaviour, IButtonCondition
{
    private DialogController _dialogController;
    private TileManager _tileManager;

    private void Start()
    {
        _dialogController = DialogController.Instance;
        _tileManager = TileManager.Instance;
    }

    public bool CanInteract()
    {
        if (_dialogController.IsOpen) return false;

        if ( _tileManager.selectedTile == null) return false;

        if (_tileManager.selectedTile.Unit) return false;

        return true;
    }
}
