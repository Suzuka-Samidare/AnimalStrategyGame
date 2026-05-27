using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemyManager : MonoBehaviour, IInitializable
{
    public static EnemyManager Instance;

    [SerializeField, Tooltip("本部データ")]
    private UnitData _hqData;

    [Header("Refs")]
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
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
    }

    public async UniTask Initialize()
    {
        ResolveDependencies();
        SpawnHqRandomTiles();
        await UniTask.CompletedTask;
    }

    private void SpawnHqRandomTiles()
    {
        List<TileController> targetTiles = new List<TileController>();
        int rows = _mapManager.enemyMapData.GetLength(0);
        int cols = _mapManager.enemyMapData.GetLength(1);
        
        // 無限ループ防止（全要素数より多く取ろうとした場合）
        int maxItems = Mathf.Min(_mapManager.maxHqCount, rows * cols);

        while (targetTiles.Count < maxItems)
        {
            int randY = Random.Range(0, rows);
            int randX = Random.Range(0, cols);
            TileController target = _mapManager.enemyMapData[randY, randX];

            if (!targetTiles.Contains(target)) // 重複チェック
            {
                targetTiles.Add(target);
            }
        }
        
        foreach (TileController tile in targetTiles)
        {
            UnitSpawnManager.Instance.SpawnUnit(tile, _hqData);
        }
    }
}
