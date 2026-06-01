using System;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class MapManager : MonoBehaviour, IInitializable
{
    public static MapManager Instance;

    [Header("Ref")]
    public GameObject playerMap;
    public GameObject enemyMap;
    public GameObject tilePrefab; // マスのPrefab

    [Header("マップデータ")]
    public TileController[,] playerMapData;
    public TileController[,] enemyMapData;

    [Header("生成情報")]
    public int mapWidth;     // マップの幅
    public int mapHeight;    // マップの高さ
    public int mapDistance = 5;

    [Header("管理ステータス")]
    public bool isDirty;
    public enum MapId
    {
        Empty = 0,
        Headquarter = 1,
        Colobus = 2,
        Gecko = 3,
        Herring = 4,
        Muskrat = 5,
        Pudu = 6,
        Sparrow = 7,
        Squid = 8,
        Taipan = 9,
        Calling = 99,
        Error = -1
    }

    [Header("集計データ")]
    [Tooltip("本部残数"), SerializeField] public int PlayerHqCount;
    [Tooltip("本部残数"), SerializeField] public int EnemyHqCount;
    
    [Header("OTHER")]
    [Tooltip("本部最大設置数")] public int maxHqCount = 2; // TODO: マップと関係ない気がするので検討

    public Action<int> OnHqCountChanged;

    void Awake()
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

    public async UniTask Initialize()
    {
        GenerateAllyMapData();
        GenerateEnemyMapData();
        await UniTask.CompletedTask;
    }

    void Update()
    {
        if (isDirty)
        {
            UpdateMapData();
        }
    }

    private void GenerateAllyMapData()
    {
        playerMapData = new TileController[mapHeight, mapWidth];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // グローバル座標の定義
                Vector3 position = new Vector3(x, 0, y);
                // Prefabをインスタンス化
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
                // 生成したタイルをMapGeneratorの子オブジェクトにする (任意、Hierarchyを整理するため)
                tile.transform.SetParent(playerMap.transform);
                tile.name = $"AllyTile_{x}_{y}";
                // 各フィールド値の更新
                TileController tileController = tile.GetComponent<TileController>();
                tileController.mapManager = this;
                tileController.globalPos = position;
                tileController.gridPos = new Vector2Int(x, y);
                tileController.SetOwner(TileController.TileOwner.Player);
                // クラスをマップデータとして格納
                playerMapData[x, y] = tileController;
            }
        }
    }

    private void GenerateEnemyMapData()
    {
        enemyMapData = new TileController[mapHeight, mapWidth];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // マスの位置を計算
                Vector3 position = new Vector3(mapWidth - 1 - x, 0, mapHeight * 2 + mapDistance - y);
                // Prefabをインスタンス化
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
                // 生成したタイルをMapGeneratorの子オブジェクトにする (任意、Hierarchyを整理するため)
                tile.transform.SetParent(enemyMap.transform);
                tile.name = $"EnemyTile_{x}_{y}";
                // 各フィールド値の更新
                TileController tileController = tile.GetComponent<TileController>();
                tileController.mapManager = this;
                tileController.globalPos = position;
                tileController.gridPos = new Vector2Int(x, y);
                tileController.SetOwner(TileController.TileOwner.Enemy);
                 // クラスをマップデータとして格納
                enemyMapData[x, y] = tileController;
            }
        }
    }

    public TileController GetPlayerMapTile(Vector2Int pos)
    {
        // 範囲外チェック（ガード句）
        if (pos.x < 0 || pos.x >= playerMapData.GetLength(0) || 
            pos.y < 0 || pos.y >= playerMapData.GetLength(1))
        {
            Debug.LogWarning($"マップ範囲外へのアクセスを検知: {pos}");
            return null; // 安全にnullを返す
        }
        return playerMapData[pos.x, pos.y];
    }

    public TileController GetEnemyMapTile(Vector2Int pos)
    {
        // 範囲外チェック（ガード句）
        if (pos.x < 0 || pos.x >= enemyMapData.GetLength(0) || 
            pos.y < 0 || pos.y >= enemyMapData.GetLength(1))
        {
            Debug.LogWarning($"マップ範囲外へのアクセスを検知: {pos}");
            return null; // 安全にnullを返す
        }
        return enemyMapData[pos.x, pos.y];
    }

    private void UpdateMapData()
    {
        PlayerHqCount = CountHeadquarters(playerMapData);
        EnemyHqCount = CountHeadquarters(enemyMapData);
        isDirty = false;

        // INITフェーズのみ実行
        OnHqCountChanged?.Invoke(PlayerHqCount);
    }

    // private void UpdateMapText()
    // {
    //     string resultText = "";
    //     for (int y = 0; y < mapHeight; y++)
    //     {
    //         string rowstr = "";
    //         for (int x = 0; x < mapWidth; x++)
    //         {
    //             MapId id;
    //             GameObject currentUnit =  playerMapData[x, y].currentUnit;
    //             if (currentUnit != null)
    //             {
    //                 id = currentUnit.GetComponent<UnitData>().id;
    //             }
    //             else
    //             {
    //                 id = MapId.Empty;
    //             }
    //             rowstr = rowstr + (int)id + " ";
    //         }
    //         resultText = rowstr + "\n" + resultText;
    //     }
    //     Debug.Log(resultText);
    // }

    public int CountHeadquarters(TileController[,] mapData)
    {
        int count = 0;
        // mapDataのサイズを動的に取得すれば、20x20以外にも対応できて超便利！
        int mapWidth = mapData.GetLength(0);
        int mapHeight = mapData.GetLength(1);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                IUnit unitBase = mapData[x, y].UnitBase;

                if (unitBase != null && unitBase.Stats.profile.id == MapId.Headquarter)
                {
                    count++;
                }
            }
        }

        if (count > maxHqCount) throw new Exception("Headquarters unit limit exceeded.");
        
        return count;
    }
}
