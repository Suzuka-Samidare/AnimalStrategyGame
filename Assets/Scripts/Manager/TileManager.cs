using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MapId = MapManager.MapId;

public class TileManager : MonoBehaviour, IInitializable
{
    public static TileManager Instance { get; private set; }

    [Header("味方マップ関連")]
    [SerializeField, Tooltip("セレクト中のタイル")]
    private Tile _selectedTile;
    public Tile selectedTile
    {
        get => _selectedTile;
        set
        {
            if (_selectedTile == value) return;
            _selectedTile = value;
            RefreshComponents();
        }
    }
    [SerializeField, Tooltip("最後にチェックした場所")]
    public Vector3 PlayerMapLastViewedPosition;

    [Header("敵マップ関連")]
    [SerializeField, Tooltip("ターゲット指定中タイル")]
    private Tile _targetTile;
    public Tile targetTile
    {
        get => _targetTile;
        set
        {
            if (_targetTile == value) return;
            _targetTile = value;
            EnemyMapLastViewedPosition = new Vector3(_targetTile.Stats.GlobalPos.x, 1f, _targetTile.Stats.GlobalPos.z);
        }
    }
    [SerializeField, Tooltip("ターゲット指定中タイル")]
    public List<Tile> targetTiles { get; private set; } = new List<Tile>();
    [Tooltip("最後にチェックした場所")]
    public Vector3 EnemyMapLastViewedPosition {get; private set; }

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
        EnemyMapLastViewedPosition = new Vector3(
            _mapManager.enemyMapData[4, 4].Stats.GlobalPos.x,
            1,
            _mapManager.enemyMapData[4, 4].Stats.GlobalPos.z
        );
        await UniTask.CompletedTask;
    }

    private void RefreshComponents()
    {
        if (selectedTile != null)
        {
            PlayerMapLastViewedPosition = new Vector3(
                selectedTile.Stats.GlobalPos.x,
                1f,
                selectedTile.Stats.GlobalPos.z
            );
        }
    }

    /// <summary>
    /// ターゲットの中心となっているタイルの登録
    /// </summary>
    public void SetTargetTile(Tile tile)
    {
        targetTile = tile;
    }

    /// <summary>
    /// 指定座標のタイルを中心に、自軍ユニット攻撃範囲を照らし合わせてターゲットタイルとして一括登録する
    /// </summary>
    public void RegisterTargetTiles(Vector2Int targetPos)
    {
        if (selectedTile == null ||
            selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new InvalidOperationException("攻撃処理を開始できません：有効な攻撃ユニットが選択されていません。");
        }

        if (targetTiles.Count > 0)
        {
            ClearTargetTiles();
        }

        List<Vector2Int> tilePositions = attackerUnit.Controller.GetTargetTilePositions(targetPos);

        foreach (Vector2Int pos in tilePositions)
        {
            Tile tile = _mapManager.GetEnemyTile(pos);

            if (tile == null) continue;

            // 新しく選択状態にする
            tile.SetTargeted(true);
            // 配列（リスト）に保存
            targetTiles.Add(tile);
        }
    }

    /// <summary>
    /// ターゲットタイル情報をクリアにする
    /// </summary>
    public void ClearTargetTiles()
    {
        DeactivateTargetFlags();
        targetTiles.Clear();
    }

    /// <summary>
    /// ターゲットとして登録されているタイル群のターゲットフラグを有効化する
    /// </summary>
    public void ActivateTargetFlags()
    {
        foreach (Tile tile in targetTiles)
        {
            if (tile == null) continue;

            tile.SetTargeted(true);
        }
    } 

    /// <summary>
    /// ターゲットとして登録されているタイル群のターゲットフラグを無効化する
    /// </summary>
    public void DeactivateTargetFlags()
    {
        foreach (Tile tile in targetTiles)
        {
            if (tile == null) continue;
            
            tile.SetTargeted(false);
        }
    }

    /// <summary>
    /// タイルを選択状態に設定する
    /// </summary>
    public void SetSelectedTile(Tile tile)
    {
        // 以前に選択されていたマスがあれば、ハイライトを解除するなどの処理
        if (selectedTile)
        {    
            selectedTile.SetSelected(false);
        }
        // 選択中タイルの更新
        selectedTile = tile;
        // 新しく選択されたマスをハイライトするなどの処理
        selectedTile.SetSelected(true);
    }

    /// <summary>
    /// 選択中のマスを解除する
    /// </summary>
    public void ClearSelectedTile()
    {
        // 以前に選択されていたマスがあれば、ハイライトを解除するなどの処理
        selectedTile.SetSelected(false);
        selectedTile = null;
    }

    /// <summary>
    /// 選択中のマス上にあるユニットを消す
    /// </summary>
    public void ClearSelectedTileOnUnit()
    {
        if (selectedTile.IsExistUnit)
        {
            Debug.Log("削除するユニットが存在しません");
            return;
        }

        UnitSpawnManager.Instance.DespawnUnit(selectedTile);
    }

    /// <summary>
    /// 選択中のマス上にあるユニットのマップIDを取得
    /// </summary>
    public MapId GetSelectedTileMapId()
    {
        if (selectedTile.Unit != null)
        {
            return selectedTile.Unit.Stats.profile.id;
        }
        else
        {
            return MapId.Empty;
        }
    }

    /// <summary>
    /// （簡易情報表示用）選択中ユニットから表示に必要な情報を取得する
    /// </summary>
    public void GetSelectedTileUnitDetail()
    {
        UnitBase unit = selectedTile.Unit;
        if (unit != null)
        {
            UnitDetailController.Instance.Open(
                unit.Stats.profile.unitName,
                unit.Stats.profile.maxHp,
                unit.Stats.hp,
                unit.Stats.profile.id == MapId.Calling
            );
        }
        else
        {
            UnitDetailController.Instance.Close();
        }
    }
}
