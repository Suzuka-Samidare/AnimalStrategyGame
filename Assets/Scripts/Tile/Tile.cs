using Cysharp.Threading.Tasks;
using UnityEngine;

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
    public UnitBase Unit
    {
        get => _unit;
        set
        {
            if (_unit == value) return;
            _unit = value;

            _isExistUnit = _unit != null;
        }
    }
    // public UnitBase Unit => _unit;
    // [Tooltip("ユニットの有無")]
    [SerializeField]
    private bool _isExistUnit;
    public bool IsExistUnit => Unit != null;

    private void Awake()
    {
        Stats = GetComponent<TileStats>();
        Controller = GetComponent<TileController>();
        View = GetComponent<TileView>();
    }

    public void SetOwner(Owner owner) => Stats.owner = owner;
    public void SetTargeted(bool isTargeted) => Stats.isTargeted = isTargeted;
    public void SetSelected(bool isSelected) => Stats.isSelected = isSelected;
    public void SetUnit(UnitBase unit) => Unit = unit;
    public void ClearUnit() => Unit = null;

    /// <summary>
    /// タイル上ユニットの位置リセット
    /// </summary>
    public void ResetUnitPosition()
    {
        if (Unit is null) return;

        Vector3 position = transform.position + Unit.Stats.profile.InitPos;
        Quaternion rotation = Stats.owner == Owner.Player ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        Unit.transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// タイル上ユニットの気絶処理（Destroy）
    /// </summary>
    public async UniTask OnFaintUnit()
    {
        // ガード区
        if (Unit is null || !Unit.Stats.IsFaint) return;

        // もし気絶アニメーションがあれば、再生する
        if (Unit.Animation)
        {
            await Unit.Animation.PlayOnceAsync(AnimationName.Death);
        }
        // デスポーン処理
        UnitSpawnManager.Instance.DespawnUnit(this);
    }



    // ====後で除外する===================================================
    // [Tooltip("ユニットオブジェクト"), SerializeField]
    // private GameObject _unitObject;
    // public GameObject unitObject
    // {
    //     get => _unitObject;
    //     set
    //     {
    //         if (_unitObject == value) return;
    //         _unitObject = value;
    //         RefreshUnit();
    //     }
    // }

    // public IUnit UnitBase => _unitBase;
    // public IAttackable UnitAttackable => _unitAttackable;
    // public IDefendable UnitDefendable => _unitDefendable;
    // public ISupportable UnitSupportable => _unitSupportable;
    // public ICallable UnitCallable => _unitCallable;
    // [Header("cache")]
    // private IUnit _unitBase;
    // private IAttackable _unitAttackable;
    // private IDefendable _unitDefendable;
    // private ISupportable _unitSupportable;
    // private ICallable _unitCallable;

    // private void RefreshUnit()
    // {
    //     _unitBase = (_unitObject != null && _unitObject.TryGetComponent<IUnit>(out var u)) ? u : null;
    //     _unitAttackable = (_unitObject != null && _unitObject.TryGetComponent<IAttackable>(out var a)) ? a : null;
    //     _unitDefendable = (_unitObject != null && _unitObject.TryGetComponent<IDefendable>(out var d)) ? d : null;
    //     _unitSupportable = (_unitObject != null && _unitObject.TryGetComponent<ISupportable>(out var s)) ? s : null;
    //     _unitCallable = (_unitObject != null && _unitObject.TryGetComponent<ICallable>(out var c)) ? c : null;
    // }
    // ====後で除外する==================================================

    // ====後で除外する その2==================================================
    public MapManager MapManager;
    // public async void DestroyUnit()
    // {
    //     Destroy(unitObject);
    //     while (unitObject != null) {
    //         await UniTask.Yield();
    //     }
    //     // マップデータの更新を促す
    //     MapManager.isDirty = true;
    // }
    // ====後で除外する その2==================================================
}
