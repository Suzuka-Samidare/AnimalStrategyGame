using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateAttackRange : MonoBehaviour, IButtonAction
{
    [SerializeField, Tooltip("最後に攻撃元になっているタイル")]
    private Tile lastSelectedTile;

    [Header("Refs")]
    private TileManager _tileManager;
    private TimelineManager _timelineManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
        _timelineManager = TimelineManager.Instance;
    }

    public void Execute()
    {
        var selectedUnitType = _tileManager.selectedTile.Unit.Stats.profile.unitType;
        var colobusCommand = _timelineManager.TryGetPlayerColobusAttackCommand();
        // 選択中の攻撃ユニットがColobusかつ、既にタイムラインにColobusの攻撃コマンドが登録されている場合
        if (selectedUnitType == UnitType.Colobus && colobusCommand != null)
        {
            _tileManager.SetTargetTile(colobusCommand.TargetTile);
            _tileManager.SetTargetTiles(colobusCommand.TargetTile.Stats.GridPos);
            lastSelectedTile = _tileManager.selectedTile;
        }

        // 選択中の味方ユニットが前回と異なるユニットの場合、攻撃範囲を再計算して範囲表示する
        if (_tileManager.targetTile && _tileManager.selectedTile != lastSelectedTile)
        {
            _tileManager.SetTargetTiles(_tileManager.targetTile.Stats.GridPos);
            lastSelectedTile = _tileManager.selectedTile;
        }
        // 同じの場合は再度同様のタイルで範囲表示する
        if (_tileManager.targetTile)
        {
            _tileManager.ActivateTargetFlags();
        }
    }
}