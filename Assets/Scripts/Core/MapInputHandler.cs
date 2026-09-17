using UnityEngine;
using Phase = GameManager.Phase;

public class MapInputHandler : MonoBehaviour
{
    [SerializeField]
    private GameManager _gameManager;
    private TileManager _tileManager;
    private UnitDetailController _unitDetailController;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        _tileManager = TileManager.Instance;
        _unitDetailController = UnitDetailController.Instance;
    }

    private void OnEnable() => InputHandler.OnSelect += HandleSelection;
    private void OnDisable() => InputHandler.OnSelect -= HandleSelection;

    private void HandleSelection(Vector2 screenPos)
    {
        // Raycastでゲームオブジェクト接触判定
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        var currentPhase = _gameManager.currentPhase;

        // 接触したオブジェクトが無い場合、タイル選択状態を解除
        if (Physics.Raycast(ray, out hit))
        {
            // TODO: コードが冗長、TimelineManagerを依存関係に含めて大丈夫か
            if (currentPhase is Phase.COMMAND)
            {
                var selectedUnitType = _tileManager.selectedTile.Unit.Stats.profile.unitType;
                var colobusCommand = TimelineManager.Instance.TryGetPlayerColobusAttackCommand();
                if (selectedUnitType == UnitType.Colobus && colobusCommand != null)
                {
                    // TODO: インフォメーションを入れる
                    return;
                }
            }

            GameObject hitObject = hit.collider.gameObject;

            // 接触対象がタイルの場合
            if (hitObject.CompareTag("Tile"))
            {
                Tile tile = hitObject.GetComponent<Tile>();

                if (tile.Stats.owner == Owner.Player)
                {
                    // タイルを選択中オブジェクトとして設定
                    _tileManager.SetSelectedTile(tile);
                }
                else
                {
                    _tileManager.SetTargetTile(tile);
                    _tileManager.SetTargetTiles(tile.Stats.GridPos);
                }

                if (tile.Unit != null)
                {
                    // ユニットアニメーション
                    if (tile.Unit.Animation) tile.Unit.Animation.PlayOnce(AnimationName.Clicked);
                    // ユニット詳細情報の表示
                    _unitDetailController.Open(tile.Unit.Stats);
                }
                else
                {
                    // ユニット詳細情報の非表示
                    _unitDetailController.Close();
                }
            }

            // Debug.Log("<color=blue>Ray判定あり & タイルではない</color>");
        }
        else
        {
            _unitDetailController.Close();

            // TODO: ここでこの処理で良いのか検討
            if (currentPhase is Phase.INIT or Phase.PREPARATION)
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
