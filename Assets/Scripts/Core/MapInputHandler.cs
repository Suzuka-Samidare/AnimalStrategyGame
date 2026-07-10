using UnityEngine;

public class MapInputHandler : MonoBehaviour
{
    [SerializeField]
    private TileManager _tileManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
        Debug.Log(_tileManager);
    }

    private void OnEnable() => InputHandler.OnSelect += HandleSelection;
    private void OnDisable() => InputHandler.OnSelect -= HandleSelection;

    private void HandleSelection(Vector2 screenPos)
    {
        // Raycastでゲームオブジェクト接触判定
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        // 接触したオブジェクトが無い場合、タイル選択状態を解除
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            // 接触対象がタイルの場合
            if (hitObject.CompareTag("Tile"))
            {
                Tile tile = hitObject.GetComponent<Tile>();

                if (tile.Stats.owner == Owner.Player)
                {
                    // タイルを選択中オブジェクトとして設定
                    _tileManager.SetSelectedTile(tile);
                    // ユニットアニメーション
                    UnitAnimation unitAnimation = hit.collider.GetComponentInChildren<UnitAnimation>();
                    if (unitAnimation) {
                        unitAnimation.PlayOnce(AnimationName.Clicked);
                    }
                }

                if (tile.Stats.owner == Owner.Enemy)
                {
                    _tileManager.SetTargetTile(tile);
                    _tileManager.RegisterTargetTiles(tile.Stats.GridPos);
                }
            }

            // 接触対象がユニットの場合
            // if (hitObject.CompareTag("Unit"))
            // {
            //     // 親要素のタイルを選択中オブジェクトとして設定
            //     _tileManager.SetSelectedTile(hitObject.transform.parent.gameObject);
            // }

            // 接触対象がタイルまたはユニットの場合
            if (hitObject.CompareTag("Tile") || hitObject.CompareTag("Unit"))
            {
                // ユニット詳細情報の表示/非表示処理
                _tileManager.GetSelectedTileUnitDetail();
            }

            // Debug.Log("<color=blue>Ray判定あり & タイルではない</color>");
        }
        else
        {
            // TODO: ここでこの処理で良いのか検討
            if (GameManager.Instance.currentPhase == GameManager.Phase.INIT ||
                GameManager.Instance.currentPhase == GameManager.Phase.PREPARATION)
            {
                if (_tileManager.selectedTile != null)
                { 
                    _tileManager.ClearSelectedTile();
                }
            }

            // // Debug.Log("Ray判定なし");
            // _tileManager.ClearSelectedTile();
        }
    }

    // void Update()
    // {
    //     if (Input.GetMouseButtonUp(0) && !CameraMovement.Instance.isDragging)
    //     {
    //         HandleClick();
    //     }
    // }

    // void HandleClick()
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

    //         // 接触対象がタイルの場合
    //         if (hitObject.CompareTag("Tile"))
    //         {
    //             _tileManager.SetSelectedTile(hitObject);
    //             UnitAnimation unitAnimation = hit.collider.GetComponentInChildren<UnitAnimation>();
    //             if (unitAnimation) {
    //                 unitAnimation.PlayOnce(AnimationName.Clicked);
    //             }
    //         }

    //         // 接触対象がユニットの場合
    //         if (hitObject.CompareTag("Unit"))
    //         {
    //             _tileManager.SetSelectedTile(hitObject.transform.parent.gameObject);
    //         }

    //         // 接触対象がタイルまたはユニットの場合
    //         if (hitObject.CompareTag("Tile") || hitObject.CompareTag("Unit"))
    //         {
    //             _tileManager.GetSelectedTileUnitDetail();
    //         }

    //         // Debug.Log("Ray判定あり & タイルではない");
    //     }
    //     else
    //     {
    //         // Debug.Log("Ray判定なし");
    //         _tileManager.ClearSelectedTile();
    //     }
    // }
}
