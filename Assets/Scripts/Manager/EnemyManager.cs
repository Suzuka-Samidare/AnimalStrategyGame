using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemyManager : MonoBehaviour, IInitializable
{
    public static EnemyManager Instance;

    [SerializeField, Tooltip("本部データ")]
    private UnitData _hqData;
    [SerializeField, Tooltip("Herringデータ")]
    private UnitData _herringData;

    [Header("Refs")]
    private UnitSpawnManager _unitSpawnManager;
    private MapManager _mapManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (_hqData == null) throw new System.Exception("HQデータが設定されていません");
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _unitSpawnManager = UnitSpawnManager.Instance;
    }

    public async UniTask Initialize()
    {
        ResolveDependencies();
        SpawnUnitRandomTiles(_hqData, _mapManager.maxHqCount);
         _unitSpawnManager.SpawnUnit(_mapManager.enemyMapData[3, 9], _herringData);
         _unitSpawnManager.SpawnUnit(_mapManager.enemyMapData[5, 4], _herringData);
        // SpawnUnitRandomTiles(_herringData, 2);
        await UniTask.CompletedTask;
    }

    private void SpawnUnitRandomTiles(UnitData unitData, int count)
    {
        List<Tile> tiles = _mapManager.GetEnemyEmptyTiles(count);

        if (tiles.Count < 1) throw new System.Exception("空きタイルが取得できませんでした。");

        for (int i = 0; i < tiles.Count; i++)
        {
            _unitSpawnManager.SpawnUnit(tiles[i], unitData);
        }
    }
}
