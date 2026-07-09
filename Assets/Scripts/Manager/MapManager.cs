using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class MapManager : MonoBehaviour, IInitializable
{
    public static MapManager Instance;

    [Header("Ref")]
    [Tooltip("プレイヤーマップオブジェクト"), SerializeField]
    private GameObject playerMap;
    [Tooltip("エネミーマップオブジェクト"), SerializeField]
    private GameObject enemyMap;

    [Header("Prefab")]
    [Tooltip("タイル本体"), SerializeField]
    private GameObject _tilePrefab; // マスのPrefab
    [Tooltip("操作不可タイル本体"), SerializeField]
    private GameObject _metaTilePrefab;

    [Header("マップデータ")]
    public Tile[,] playerMapData;
    public Tile[,] enemyMapData;

    [Header("生成情報")]
    [Tooltip("マップの幅")]
    public int mapWidth;
    [Tooltip("マップの高さ")]
    public int mapHeight;
    [Tooltip("マップ間の距離")]
    public int mapDistance = 10;
    [Tooltip("操作不可マップの高さ"), SerializeField]
    private int _metaMapHeight = 5;
    [Tooltip("陣営ごとの最終的なマップの高さ")]
    public int TotalMapHeight => mapHeight + _metaMapHeight;

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

    [Header("Action")]
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

    /// <summary>
    /// 味方マップの生成
    /// </summary>
    private void GenerateAllyMapData()
    {
        playerMapData = new Tile[mapWidth, mapHeight + _metaMapHeight];

        for (int y = 0; y < mapHeight + _metaMapHeight; y++)
        {
            // mapHeightを上回る座標のタイルはメタタイルとして登録
            GameObject prefab = y < mapHeight ? _tilePrefab : _metaTilePrefab;
            for (int x = 0; x < mapWidth; x++)
            {
                // Prefabをインスタンス化
                GameObject tileObj = Instantiate(
                    prefab,
                    new Vector3(x, 0, y),
                    Quaternion.identity
                );
                // 生成したタイルをMapGeneratorの子オブジェクトにする (任意、Hierarchyを整理するため)
                tileObj.transform.SetParent(playerMap.transform);
                // tile.name = $"PlayerTile_{x}_{y}";
                tileObj.name = "Player" + (y < mapHeight ? "" : "Meta") + $"_{x}_{y}";
                // 各フィールド値の更新
                Tile tile = tileObj.GetComponent<Tile>();
                tile.MapManager = this;
                tile.Stats.GlobalPos = tileObj.transform.position;
                tile.Stats.GridPos = new Vector2Int(x, y);
                tile.SetOwner(Owner.Player);
                // クラスをマップデータとして格納
                playerMapData[x, y] = tile;
            }
        }
    }

    /// <summary>
    /// 敵マップの生成
    /// </summary>
    private void GenerateEnemyMapData()
    {
        enemyMapData = new Tile[mapWidth, mapHeight + _metaMapHeight];

        for (int y = 0; y < mapHeight + _metaMapHeight; y++)
        {
            // mapHeightを上回る座標のタイルはメタタイルとして登録
            GameObject prefab = y < mapHeight ? _tilePrefab : _metaTilePrefab;
            for (int x = 0; x < mapWidth; x++)
            {
                // Prefabをインスタンス化
                GameObject tileObj = Instantiate(
                    prefab,
                    new Vector3(mapWidth - 1 - x, 0, mapHeight * 2 + mapDistance - y),
                    Quaternion.identity
                );
                // 生成したタイルをMapGeneratorの子オブジェクトにする (任意、Hierarchyを整理するため)
                tileObj.transform.SetParent(enemyMap.transform);
                // tile.name = $"EnemyTile_{x}_{y}";
                tileObj.name = "Enemy" + (y < mapHeight ? "" : "Meta") + $"_{x}_{y}";
                // 各フィールド値の更新
                Tile tile = tileObj.GetComponent<Tile>();
                tile.MapManager = this;
                tile.Stats.GlobalPos = tileObj.transform.position;
                tile.Stats.GridPos = new Vector2Int(x, y);
                tile.SetOwner(Owner.Enemy);
                 // クラスをマップデータとして格納
                enemyMapData[x, y] = tile;
            }
        }
    }

    /// <summary>
    /// 味方マップのTileを取得する（メタタイルもオプション指定で取得可能）
    /// </summary>
    public Tile GetPlayerTile(Vector2Int pos, bool isCalculableMetaTile = false)
    {
        int heightLength =
            isCalculableMetaTile ? playerMapData.GetLength(1) : playerMapData.GetLength(1) - _metaMapHeight;
        // 範囲外チェック（ガード句）
        if (pos.x < 0 || pos.x >= playerMapData.GetLength(0) || 
            pos.y < 0 || pos.y >= heightLength)
        {
            // Debug.LogWarning($"マップ範囲外へのアクセスを検知: {pos}");
            return null;
        }
        return playerMapData[pos.x, pos.y];
    }

    /// <summary>
    /// プレイヤーマップのTileリストを取得する
    /// </summary>
    public List<Tile> GetPlayerTiles(List<Vector2Int> positions, bool isCalculableMetaTile = false)
    {
        List<Tile> result = new List<Tile>();
        foreach(var pos in positions)
        {
            Tile tile = GetPlayerTile(pos, isCalculableMetaTile);
            if (tile != null) result.Add(tile);
        }
        return result;
    }

    /// <summary>
    /// 敵マップのTileを取得する（メタタイルもオプション指定で取得可能）
    /// </summary>
    public Tile GetEnemyTile(Vector2Int pos, bool isCalculableMetaTile = false)
    {
        int heightLength =
            isCalculableMetaTile ? enemyMapData.GetLength(1) : enemyMapData.GetLength(1) - _metaMapHeight;
        // 範囲外チェック（ガード句）
        if (pos.x < 0 || pos.x >= enemyMapData.GetLength(0) || 
            pos.y < 0 || pos.y >= heightLength)
        {
            // Debug.LogWarning($"マップ範囲外へのアクセスを検知: {pos}");
            return null;
        }
        return enemyMapData[pos.x, pos.y];
    }

    /// <summary>
    /// 敵マップのTileリストを取得する
    /// </summary>
    public List<Tile> GetEnemyTiles(List<Vector2Int> positions, bool isCalculableMetaTile = false)
    {
        List<Tile> result = new List<Tile>();
        foreach(var pos in positions)
        {
            Tile tile = GetEnemyTile(pos, isCalculableMetaTile);
            if (tile != null) result.Add(tile);
        }
        return result;
    }

    /// <summary>
    /// マップデータ更新、集計
    /// </summary>
    private void UpdateMapData()
    {
        PlayerHqCount = CountHeadquarters(playerMapData);
        EnemyHqCount = CountHeadquarters(enemyMapData);
        isDirty = false;

        // INITフェーズのみ実行
        OnHqCountChanged?.Invoke(PlayerHqCount);
    }

    /// <summary>
    /// 指定したマップ上にある本部ユニット数を取得
    /// </summary>
    public int CountHeadquarters(Tile[,] mapData)
    {
        int count = 0;
        // mapDataのサイズを動的に取得すれば、20x20以外にも対応できて超便利！
        int mapWidth = mapData.GetLength(0);
        int mapHeight = mapData.GetLength(1);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                UnitBase unit = mapData[x, y].Unit;

                if (unit != null && unit.Stats.profile.id == MapId.Headquarter)
                {
                    count++;
                }
            }
        }

        if (count > maxHqCount) throw new Exception("Headquarters unit limit exceeded.");
        return count;
    }

    /// <summary>
    /// 敵マップ上でユニット配置されていないタイルの取得
    /// </summary>
    public List<Tile> GetEnemyEmptyTiles(int count)
    {
        List<Tile> emptyTiles = new List<Tile>();
        List<Tile> resultTiles = new List<Tile>();
        int rows = enemyMapData.GetLength(0);
        int cols = enemyMapData.GetLength(1);

        // タイルがnullでないかつユニットが配置されていない場合は、空きタイルとしてリストに追加
        ForEachTile((x, y) =>
        {
            Tile tile = enemyMapData[x, y];
            if (tile != null && tile.Unit == null) emptyTiles.Add(tile);
        });

        if (emptyTiles.Count == 0)
        {
            Debug.LogWarning("EnemyMapに空きタイルがありません。");
            return null;
        }

        int roopCount = Mathf.Min(count, emptyTiles.Count);
        for (int i = 0; i < roopCount; i++)
        {
            // 残っている空きマスからランダムにインデックスを選択
            int index = UnityEngine.Random.Range(0, emptyTiles.Count);
            // 選ばれたタイルを結果リストに追加
            resultTiles.Add(emptyTiles[index]);
            // 同じタイルを二度選ばないように、候補リストから削除
            emptyTiles.RemoveAt(index);
        }

        return resultTiles;
    }

    /// <summary>
    /// 味方マップ上のHerringユニットが配置されているタイルの全取得
    /// </summary>
    public List<Tile> GetPlayerMapHerringTiles()
    {
        List<Tile> tiles = new List<Tile>();
        ForEachTile((x, y) =>
        {
            UnitBase unit = playerMapData[x, y].Unit;
            if (unit != null && unit.Stats.profile.unitType == UnitType.Herring)
            {
                tiles.Add(playerMapData[x, y]);
            }
        });

        return tiles;
    }

    /// <summary>
    /// 敵マップ上Herringユニットが配置されているタイルの全取得
    /// </summary>
    public List<Tile> GetEnemyMapHerringTiles()
    {
        List<Tile> tiles = new List<Tile>();
        ForEachTile((x, y) =>
        {
            UnitBase unit = enemyMapData[x, y].Unit;
            if (unit != null && unit.Stats.profile.unitType == UnitType.Herring) 
            {
                tiles.Add(enemyMapData[x, y]);
            }
        });

        return tiles;
    }

    /// <summary>
    /// 攻撃物がターゲットに着弾するまでに通過するタイルの取得（メタタイルを含む）
    /// </summary>
    public List<Tile> GetTrajectoryTiles(Tile target)
    {
        Tile[,] mapData = target.Stats.owner == Owner.Player ? playerMapData : enemyMapData;
        List<Tile> tiles = new List<Tile>();

        for (int y = target.Stats.GridPos.y; y < mapHeight + _metaMapHeight; y++)
        {
            int x = target.Stats.GridPos.x;
            if (mapData[x, y] != null) tiles.Add(mapData[x, y]);
        }

        return tiles;
    }

    /// <summary>
    /// マップ検索処理汎用メソッド
    /// </summary>
    private void ForEachTile(Action<int, int> action)
    {
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                action?.Invoke(x, y);
            }
        }
    }
}
