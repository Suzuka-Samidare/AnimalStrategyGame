using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
    public void SpawnUnit(Tile tile, UnitData unitData)
    {
        // DEBUG ============================================================
        // Debug.Log("SpawnUnit");
        // DEBUG ============================================================

        if (tile.Unit != null) return;

        Owner tileOwner = tile.Stats.owner;
        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.Stats.owner == Owner.Player) ? playerPool : enemyPool;
        // スポーン処理
        Vector3 tilePosition = tile.transform.position;
        Vector3 unitPosition  = tilePosition + unitData.initPos;
        Quaternion unitRotation = tile.Stats.owner == Owner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        UnitBase unit = targetPool.Spawn(
            unitData.profile.unitType,
            unitPosition,
            unitRotation
        );
        // タイルにユニットオブジェクトを紐づけ
        tile.SetUnit(unit);
        // ユニット情報の初期化
        tile.Unit.Setup(tileOwner, unitData);
        // マップデータの更新を促す
        _mapManager.isDirty = true;
    }

    public void SpawnUnitDelayed(Tile tile, UnitData unitData)
    {
        // DEBUG ============================================================
        // Debug.Log("SpawnUnitDelayed");
        // DEBUG ============================================================

        if (tile.Unit != null) return;

        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.Stats.owner == Owner.Player) ? playerPool : enemyPool;
        // スポーン処理
        Vector3 tilePosition = tile.transform.position;
        Vector3 unitPosition = tilePosition + unitData.initPos;
        Quaternion unitRotation = tile.Stats.owner == Owner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        UnitBase unit = targetPool.Spawn(
            unitData.callingProfile.unitType,
            unitPosition,
            unitRotation
        );
        // タイルにユニットオブジェクトを紐づけ
        tile.SetUnit(unit);
        // ユニット情報の初期化
        tile.Unit.Setup(tile.Stats.owner, unitData);
        // 呼び出し完了時の処理
        Action onCompleteCallback = () =>
        {
            // DEBUG ============================================================
            // Debug.Log("onCompleteCallback");
            // if (tile.Unit == null)
            // {
            //     Debug.LogError($"???????????");
            // }
            // DEBUG ============================================================

            // 仮ユニットの除去
            DespawnUnit(tile);
            // 本命ユニットの作成
            SpawnUnit(tile, unitData);
        };
        // 呼び出しタイマー開始
        if (unit is CallUnit callUnit && callUnit.Controller is CallController callController)
        {    
            callController.StartTimer(unitData.callTime, onCompleteCallback);
        }
        // マップデータの更新を促す
        _mapManager.isDirty = true;
    }

    public void DespawnUnit(Tile tile)
    {
        // DEBUG ============================================================
        // Debug.Log("DespawnUnit");
        // if (tile.Unit.Stats == null)
        // {
        //     Debug.LogError($"タイルのユニットデータが足りないよ！");
        //     return;
        // }
        // DEBUG ============================================================

        // オーナー情報からプール先の設定
        FactionUnitPool targetPool = (tile.Stats.owner == Owner.Player) ? playerPool : enemyPool;
        // プール回収するユニットタイプの取得
        UnitType unitType = tile.Unit.Stats.profile.unitType;
        // デスポーン処理
        if (tile.Unit is CallUnit callUnit)
        {
            callUnit.Controller.ClearActiveTimer();
        }
        targetPool.Despawn(unitType, tile.Unit);
        tile.ClearUnit();
        // マップデータの更新を促す
        _mapManager.isDirty = true;

        // // BaseUnitコンポーネントを取得
        // if (tile.unitObject.TryGetComponent<BaseUnit>(out var unit))
        // {   
        //     // デスポーン処理
        //     targetPool.Despawn(unitType, unit);
        //     tile.unitObject = null;
        //     // マップデータの更新を促す
        //     _mapManager.isDirty = true;
        // }
        // else
        // {
        //     throw new Exception("BaseUnitコンポーネントがアタッチされていないため、Despawn処理を中止します。");
        // }
    }

    public void DespawnAllUnit()
    {
        for (int y = 0; y < _mapManager.mapHeight; y++)
        {
            for (int x = 0; x < _mapManager.mapWidth; x++)
            {
                Tile tile = _mapManager.playerMapData[x, y];
                if (tile.Unit)
                {
                    DespawnUnit(tile);
                }
            }
        }
    }
}