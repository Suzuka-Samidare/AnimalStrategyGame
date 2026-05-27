using UnityEngine;

public class UnitCaller : MonoBehaviour, IButtonAction {
    [SerializeField, Tooltip("ユニットデータ")]
    private UnitData unitData;

    [Header("Refs")]
    private TileManager _tileManager;
    private UnitSpawnManager _unitSpawnManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
        _unitSpawnManager = UnitSpawnManager.Instance;
    }

    public void Execute() {
        if (unitData.callTime > 0)
        {
            Debug.Log("SpawnUnitOnSelectedUnit: 待ち時間ありのユニットです。");
            _unitSpawnManager.SpawnUnitDelayed(_tileManager.selectedTileController, unitData);
        }
        else
        {
            Debug.Log("SpawnUnitOnSelectedUnit: 待ち時間なしのユニットです。");
            _unitSpawnManager.SpawnUnit(_tileManager.selectedTileController, unitData);
        }
    }
}