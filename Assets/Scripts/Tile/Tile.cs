using Cysharp.Threading.Tasks;
using UnityEngine;
using TileOwner = TileStats.TileOwner;

[RequireComponent(typeof(TileStats))]
[RequireComponent(typeof(TileController))]
[RequireComponent(typeof(TileView))]
public class Tile : MonoBehaviour
{
    [Header("Refs")]
    public TileStats Stats;
    public TileController Controller;
    public TileView View;

    [Header("ユニット関連")]
    [SerializeField, Tooltip("現在配置されているユニット")]
    private UnitBase _unit;
    public UnitBase Unit => _unit;
    [Tooltip("ユニットの有無")]
    // public bool IsExistUnit => Unit != null;
    public bool IsExistUnit => unitObject != null;

    private void Awake()
    {
        Stats = GetComponent<TileStats>();
        Controller = GetComponent<TileController>();
        View = GetComponent<TileView>();
    }

    public void SetOwner(TileOwner owner) => Stats.owner = owner;
    public void SetTargeted(bool isTargeted) => Stats.isTargeted = isTargeted;
    public void SetSelected(bool isSelected) => Stats.isSelected = isSelected;
    public void SetUnit(UnitBase unit) => _unit = unit;
    public void ClearUnit() => _unit = null;

    

    // ====後で除外する===================================================
    [Tooltip("ユニットオブジェクト"), SerializeField]
    private GameObject _unitObject;
    public GameObject unitObject
    {
        get => _unitObject;
        set
        {
            if (_unitObject == value) return;
            _unitObject = value;
            RefreshUnit();
        }
    }

    public IUnit UnitBase => _unitBase;
    public IAttackable UnitAttackable => _unitAttackable;
    public IDefendable UnitDefendable => _unitDefendable;
    public ISupportable UnitSupportable => _unitSupportable;
    public ICallable UnitCallable => _unitCallable;
    [Header("cache")]
    private IUnit _unitBase;
    private IAttackable _unitAttackable;
    private IDefendable _unitDefendable;
    private ISupportable _unitSupportable;
    private ICallable _unitCallable;

    private void RefreshUnit()
    {
        _unitBase = (_unitObject != null && _unitObject.TryGetComponent<IUnit>(out var u)) ? u : null;
        _unitAttackable = (_unitObject != null && _unitObject.TryGetComponent<IAttackable>(out var a)) ? a : null;
        _unitDefendable = (_unitObject != null && _unitObject.TryGetComponent<IDefendable>(out var d)) ? d : null;
        _unitSupportable = (_unitObject != null && _unitObject.TryGetComponent<ISupportable>(out var s)) ? s : null;
        _unitCallable = (_unitObject != null && _unitObject.TryGetComponent<ICallable>(out var c)) ? c : null;
    }
    // ====後で除外する==================================================

    // ====後で除外する その2==================================================
    public MapManager MapManager;
    public async void DestroyUnit()
    {
        Destroy(unitObject);
        while (unitObject != null) {
            await UniTask.Yield();
        }
        // マップデータの更新を促す
        MapManager.isDirty = true;
    }
    // ====後で除外する その2==================================================
}
