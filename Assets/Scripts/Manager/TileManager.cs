using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MapId = MapManager.MapId;

public class TileManager : MonoBehaviour, IInitializable
{
    public static TileManager Instance;

    [Header("味方マップ関連")]
    // TODO: 型をGameObjectではなく、TileControllerに出来ないか検討する。
    [SerializeField, Tooltip("セレクト中のタイル")]
    private GameObject _selectedTile;
    public GameObject selectedTile
    {
        get => _selectedTile;
        set
        {
            if (_selectedTile == value) return;
            _selectedTile = value;
            RefreshComponents();
        }
    }
    [field: SerializeField, Tooltip("セレクト中のタイルコントローラ")]
    public TileController selectedTileController { get; private set; }
    [SerializeField, Tooltip("アクセス可否")]
    private bool _canAccessSelectedTileController => selectedTile != null && selectedTileController != null;
    [SerializeField, Tooltip("最後にチェックした場所")]
    public Vector3 PlayerMapLastViewedPosition;

    [Header("敵マップ関連")]
    [SerializeField, Tooltip("ターゲット指定中タイル")]
    private TileController _targetTile;
    public TileController targetTile
    {
        get => _targetTile;
        set
        {
            if (_targetTile == value) return;
            _targetTile = value;
            EnemyMapLastViewedPosition = new Vector3(_targetTile.globalPos.x, 1f, _targetTile.globalPos.z);
        }
    }
    [SerializeField, Tooltip("ターゲット指定中タイル")]
    public List<TileController> targetTiles { get; private set; } = new List<TileController>();
    [SerializeField, Tooltip("最後にチェックした場所")]
    public Vector3 EnemyMapLastViewedPosition;

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
         await UniTask.CompletedTask;
    }

    private void RefreshComponents()
    {
        if (selectedTile != null)
        {
            selectedTileController = selectedTile.GetComponent<TileController>();
            PlayerMapLastViewedPosition = new Vector3(
                selectedTileController.globalPos.x,
                1f,
                selectedTileController.globalPos.z
            );
        }
        else
        {
            selectedTileController = null;
        }
    }

    // public void SetTargetTile(Vector2Int pos)
    // {
    //     targetTile =  _mapManager.enemyMapData[pos.x, pos.y];
    // }
    public void SetTargetTile(TileController tileController)
    {
        targetTile = tileController;
    }

    public void RegisterTargetTiles(Vector2Int targetPos)
    {
        if (!_canAccessSelectedTileController) return;

        if (targetTiles.Count > 0)
        {
            ClearTargetTiles();
        }

        if (selectedTileController.UnitAttackable == null)
        {
            throw new Exception("UnitControllerがありません");
        }

        List<Vector2Int> tilePositions = selectedTileController.UnitAttackable.Controller.GetTargetTilePositions(targetPos);

        foreach (Vector2Int pos in tilePositions)
        {
            TileController tileController = _mapManager.GetEnemyTile(pos);

            if (tileController != null)
            {
                // 新しく選択状態にする
                tileController.isTargeted = true;
                // 配列（リスト）に保存
                targetTiles.Add(tileController);
            }
        }
    }

    public void ClearTargetTiles()
    {
        DeactivateTargetFlags();
        targetTiles.Clear();
    }

    public void ActivateTargetFlags()
    {
        foreach (TileController tileController in targetTiles)
        {
            if (tileController != null)
            {
                tileController.isTargeted = true;
            }
        }
    } 

    public void DeactivateTargetFlags()
    {
        foreach (TileController tileController in targetTiles)
        {
            if (tileController != null)
            {
                tileController.isTargeted = false;
            }
        }
    }

    // // TODO: MapManagerにするか検討する。
    // public TileController GetPlayerTile(Vector2Int pos)
    // {
    //     return _mapManager.playerMapData[pos.x, pos.y];
    // }


    // void Update()
    // {
    //     if (Input.GetMouseButtonUp(0) && !CameraMovement.Instance.isDragging)
    //     {
    //         CheckMouseDown();
    //     }
    // }

    // void CheckMouseDown()
    // {
    //     // UI要素を選択またはフォーカスしている場合は処理を進行しない
    //     if (EventSystem.current.IsPointerOverGameObject()) return;

    //     // Raycastでゲームオブジェクト接触判定
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     RaycastHit hit;

    //     // 接触したオブジェクトが無い場合、タイル選択状態を解除
    //     if (Physics.Raycast(ray, out hit))
    //     {
    //         GameObject hitObject = hit.collider.gameObject;

    //         // 接触オブジェクトがタイルまたはユニットだった場合、選択状態に更新
    //         if (hitObject.CompareTag("Tile"))
    //         {
    //             // タイルを引数にして処理
    //             SetSelectedTile(hitObject);
    //         }
    //         else if (hitObject.CompareTag("Unit"))
    //         {
    //             // ユニットの親要素であるタイルを引数にして処理
    //             SetSelectedTile(hitObject.transform.parent.gameObject);
    //         }
    //         else
    //         {
    //             // Debug.Log("Ray判定あり & タイルではない");
    //         }
    //     }
    //     else
    //     {
    //         // Debug.Log("Ray判定なし");
    //         ClearSelectedTile();
    //     }
    // }

    // タイルを選択状態に設定する
    public void SetSelectedTile(GameObject tile)
    {
        // 以前に選択されていたマスがあれば、ハイライトを解除するなどの処理
        if (_canAccessSelectedTileController)
        {
            selectedTileController.isSelected = false;
        }

        selectedTile = tile;

        // 新しく選択されたマスをハイライトするなどの処理
        if (_canAccessSelectedTileController)
        {
            selectedTileController.isSelected = true;
        }
    }

    // 選択中のマスを解除する
    public void ClearSelectedTile()
    {
        // 以前に選択されていたマスがあれば、ハイライトを解除するなどの処理
        if (_canAccessSelectedTileController)
        {
            selectedTileController.isSelected = false;
        }

        selectedTile = null;
    }

    // 選択中のマス上にあるユニットを消す
    public void ClearSelectedTileOnUnit()
    {
        if (!selectedTileController.isExistUnit)
        {
            Debug.Log("削除するユニットが存在しません");
            return;
        }

        selectedTileController.DestroyUnit();
    }

    // 選択中のマス上にあるユニットのマップIDを取得
    public MapId GetSelectedTileMapId()
    {
        if (selectedTileController.unitObject != null)
        {
            return selectedTileController.unitMapId;
        }
        else
        {
            return MapId.Empty;
        }
    }

    public void GetSelectedTileUnitDetail()
    {
        IUnit unitBase = selectedTileController.UnitBase;
        if (unitBase != null)
        {
            UnitDetailController.Instance.Open(
                unitBase.Stats.profile.unitName,
                unitBase.Stats.profile.maxHp,
                unitBase.Stats.hp,
                unitBase.Stats.profile.id == MapId.Calling
            );
        }
        else
        {
            UnitDetailController.Instance.Close();
        }
    }
}
