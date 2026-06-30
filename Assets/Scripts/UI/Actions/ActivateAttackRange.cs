using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateAttackRange : MonoBehaviour, IButtonAction
{
    [SerializeField, Tooltip("最後に攻撃元になっているタイル")]
    private Tile lastSelectedTile;

    [Header("Refs")]
    private TileManager _tileManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
    }

    public void Execute()
    {
        if (_tileManager.targetTile && _tileManager.selectedTile != lastSelectedTile)
        {
            _tileManager.RegisterTargetTiles(_tileManager.targetTile.Stats.GridPos);
            lastSelectedTile = _tileManager.selectedTile;
        }
        if (_tileManager.targetTile)
        {
            _tileManager.ActivateTargetFlags();
        }
    }
}