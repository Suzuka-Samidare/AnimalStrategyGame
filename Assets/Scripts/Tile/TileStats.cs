using UnityEngine;

public class TileStats : MonoBehaviour
{
    [Header("タイルステータス")]
    [SerializeField, Tooltip("選択状態の有無")]
    private bool _isSelected;
    public bool isSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;

            if (_tileView != null) _tileView.RefreshVisual();
        }
    }
    [SerializeField, Tooltip("ターゲット状態の有無")]
    private bool _isTargeted;
    public bool isTargeted
    {
        get => _isTargeted;
        set
        {
            if (_isTargeted == value) return;
            _isTargeted = value;

            if (_tileView != null) _tileView.RefreshVisual();
        }
    }
    [Tooltip("敵から視認可能か")]
    public bool isRevealed = false;
    [Tooltip("タイルの陣地種別")]
    public Owner owner;

    [Header("タイル座標情報")]
    [Tooltip("ワールド座標")]
    public Vector3 GlobalPos;
    [Tooltip("マップ上座標")]
    public Vector2Int GridPos;

    [Header("Refs")]
    private TileView _tileView;

    private void Awake()
    {
        _tileView = GetComponent<TileView>();
    }

    // [Tooltip("敵から視認可能か")]
    // public bool isRevealed = false;
}
// 