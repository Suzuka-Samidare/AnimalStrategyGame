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

    private void OnEnable() => InputHandler.OnSelect += ProcessRaycastDetection;
    private void OnDisable() => InputHandler.OnSelect -= ProcessRaycastDetection;

    private void ProcessRaycastDetection(Vector2 screenPos)
    {
        // Raycastでゲームオブジェクト接触判定
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        Phase currentPhase = _gameManager.currentPhase;

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
                HandleTileContact(tile);
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

    private void HandleTileContact(Tile tile)
    {
        // プレイヤー側のタイルの場合
        if (tile.Stats.owner == Owner.Player)
        {
            _tileManager.SetSelectedTile(tile);

            if (tile.Unit != null)
            {
                _unitDetailController.Open(tile.Unit.Stats);
                if (tile.Unit.Animation)
                {
                    tile.Unit.Animation.PlayOnce(AnimationName.Clicked);
                }
            }
        }
        // 敵側のタイルの場合
        else
        {
            _tileManager.SetTargetTile(tile);
            _tileManager.SetTargetTiles(tile.Stats.GridPos);

            if (tile.Unit != null && tile.Unit.Stats.IsVisible)
            {
                _unitDetailController.Open(tile.Unit.Stats);
            }
        }

        // ユニットがいない場合は詳細情報の非表示（共通処理）
        if (tile.Unit == null) _unitDetailController.Close();
    }
}
