using UnityEngine;

[RequireComponent(typeof(UnitCallerCondition))]
public class UnitCaller : MonoBehaviour, IButtonAction {
    [SerializeField, Tooltip("ユニットデータ")]
    private UnitData _unitData;

    [Header("Refs")]
    private TileManager _tileManager;
    private UnitSpawnManager _unitSpawnManager;
    private PlayerManager _playerManager;
    private UnitCallerCondition _unitCallerCondition;

    private void Awake()
    {
        _unitCallerCondition = GetComponent<UnitCallerCondition>();
        _unitCallerCondition.Initialize(_unitData);
    }

    private void Start()
    {
        _tileManager = TileManager.Instance;
        _unitSpawnManager = UnitSpawnManager.Instance;
        _playerManager = PlayerManager.Instance;
    }

    public void Execute() {
        if (_playerManager.animalPoint < _unitData.cost) throw new System.Exception("呼出するための条件を満たしていません。");

        _playerManager.UseAnimalPoint(_unitData.cost);

        if (_unitData.callTime > 0)
        {
            // Debug.Log("SpawnUnitOnSelectedUnit: 待ち時間ありのユニットです。");
            _unitSpawnManager.SpawnUnitDelayed(_tileManager.selectedTile, _unitData);
        }
        else
        {
            // Debug.Log("SpawnUnitOnSelectedUnit: 待ち時間なしのユニットです。");
            _unitSpawnManager.SpawnUnit(_tileManager.selectedTile, _unitData);
        }
    }
}