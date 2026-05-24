using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TileOwner = TileController.TileOwner;

public class UnitSpawnManager : MonoBehaviour
{
    public static UnitSpawnManager Instance { get; private set; }

    [SerializeField] private FactionUnitPool playerPool;
    [SerializeField] private FactionUnitPool enemyPool;

    [Header("Refs")]
    private MapManager _mapManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _mapManager = MapManager.Instance;
    }

    /// <summary>
    /// プールからユニットの呼び出し
    /// </summary>
    public void SpawnUnit(TileController tile, BaseUnitData unitData)
    {
        // DEBUG ============================================================
        Debug.Log("SpawnUnit");
        // DEBUG ============================================================

        if (tile.unitObject != null) return;

        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.owner == TileOwner.Player) ? playerPool : enemyPool;
        // スポーン処理
        Vector3 tilePosition = tile.transform.position;
        Vector3 unitPosition = new Vector3(tilePosition.x, unitData.initPos.y, tilePosition.z);
        Quaternion unitRotation = tile.owner == TileOwner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        GameObject unit = targetPool.Spawn(
            unitData.profile.unitType,
            unitPosition,
            unitRotation
        );
        // タイルにユニットオブジェクトを紐づけ
        tile.unitObject = unit;
        // ユニット情報の初期化
        tile.unitStats.Initialize(unitData.profile);
        // マップデータの更新を促す
        _mapManager.isDirty = true;
    }

    public void SpawnUnitDelayed(TileController tile, BaseUnitData unitData)
    {
        // DEBUG ============================================================
        Debug.Log("SpawnUnitDelayed");
        // DEBUG ============================================================

        if (tile.unitObject != null) return;

        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.owner == TileOwner.Player) ? playerPool : enemyPool;
        // スポーン処理
        Vector3 tilePosition = tile.transform.position;
        Vector3 unitPosition = new Vector3(tilePosition.x, 0.75f, tilePosition.z);
        Quaternion unitRotation = tile.owner == TileOwner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        GameObject unit = targetPool.Spawn(
            unitData.callingProfile.unitType,
            unitPosition,
            unitRotation
        );
        // タイルにユニットオブジェクトを紐づけ
        tile.unitObject = unit;
        // ユニット情報の初期化
        tile.unitStats.Initialize(unitData.callingProfile);
        // 呼び出し完了時の処理
        Action onCompleteCallback = () =>
        {
            // DEBUG ============================================================
            Debug.Log("onCompleteCallback");
            if (tile.unitObject == null)
            {
                Debug.LogError($"???????????");
                // return;
            }
            // DEBUG ============================================================

            // 仮ユニットの除去
            DespawnUnit(tile);
            // 本命ユニットの作成
            SpawnUnit(tile, unitData);
        };
        // 呼び出しタイマー開始
        tile.callingUnitController.StartTimer(unitData.callTime, onCompleteCallback);
        // マップデータの更新を促す
        _mapManager.isDirty = true;
    }

    public void DespawnUnit(TileController tile)
    {
        // DEBUG ============================================================
        Debug.Log("DespawnUnit");
        if (tile.unitStats == null)
        {
            Debug.LogError($"タイルのユニットデータが足りないよ！ stats:{tile.unitStats}, profile:{tile.unitStats?.profile}");
            return;
        }
        // DEBUG ============================================================

        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.owner == TileOwner.Player) ? playerPool : enemyPool;
        // プール回収するユニットタイプの取得
        UnitType unitType = tile.unitStats.profile.unitType;
        // デスポーン処理
        targetPool.Despawn(unitType, tile.unitObject);
        tile.unitObject = null;
        // 処理待ち
        // await UniTask.WaitUntil(() => tile.unitObject == null);
        // マップデータの更新を促す
        _mapManager.isDirty = true;
    }

    public void DespawnAllUnit()
    {
        for (int y = 0; y < _mapManager.mapHeight; y++)
        {
            for (int x = 0; x < _mapManager.mapWidth; x++)
            {
                TileController tile = _mapManager.playerMapData[x, y];
                if (tile.unitObject)
                {
                    DespawnUnit(tile);
                }
            }
        }
    }
}