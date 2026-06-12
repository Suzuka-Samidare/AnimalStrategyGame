using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using MapId = MapManager.MapId;

public class TileController : MonoBehaviour
{
    public enum TileOwner { Player, Enemy }
    
    [Header("視認タイマー関連")]
    [Tooltip("視認時間"), SerializeField]
    private float revealDuration = 5.0f;
    private float _timer = 5.0f;

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
    public TileOwner owner;

    [Header("タイル座標情報")]
    [Tooltip("ワールド座標")]
    public Vector3 GlobalPos;
    [Tooltip("マップ上座標")]
    public Vector2Int gridPos;

    [Header("ユニット情報")]
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
    // [field: SerializeField]
    // public UnitStats unitStats { get; private set; }
    // [field: SerializeField]
    // public UnitController unitController { get; private set; }
    // [field: SerializeField]
    // public AttackUnitController attackUnitController { get; private set; }
    [field: SerializeField]
    public CallingUnitController callingUnitController { get; private set; }
    
    public IUnit UnitBase => _unitBase;
    public IAttackable UnitAttackable => _unitAttackable;
    public IDefendable UnitDefendable => _unitDefendable;
    public ISupportable UnitSupportable => _unitSupportable;
    public ICallable UnitCallable => _unitCallable;

    [Tooltip("ユニットの有無")]
    public bool isExistUnit => unitObject;
    [Tooltip("マップID")]
    public MapId unitMapId => UnitBase != null ? UnitBase.Stats.profile.id : MapId.Empty;

    [Header("cache")]
    private IUnit _unitBase;
    private IAttackable _unitAttackable;
    private IDefendable _unitDefendable;
    private ISupportable _unitSupportable;
    private ICallable _unitCallable;

    [Header("Refs")]
    private TileView _tileView;
    public MapManager mapManager;

    void Awake()
    {
        _tileView = GetComponent<TileView>();
    }

    void Update()
    {
        // UpdateTileVisual();
        UpdateRevealTimer();
    }

    private void RefreshUnit()
    {
        // 前回の回答で決めた「最強の条件式（三項演算子）」で一気にキャッシュを更新！
        _unitBase = (_unitObject != null && _unitObject.TryGetComponent<IUnit>(out var u)) ? u : null;
        _unitAttackable = (_unitObject != null && _unitObject.TryGetComponent<IAttackable>(out var a)) ? a : null;
        _unitDefendable = (_unitObject != null && _unitObject.TryGetComponent<IDefendable>(out var d)) ? d : null;
        _unitSupportable = (_unitObject != null && _unitObject.TryGetComponent<ISupportable>(out var s)) ? s : null;
        _unitCallable = (_unitObject != null && _unitObject.TryGetComponent<ICallable>(out var c)) ? c : null;
    }

    public void SetOwner(TileOwner owner)
    {
        this.owner = owner;
    }

    public void Reveal()
    {
        // すでに true の場合でも、タイマーを初期値（最大値）にリセットする
        isRevealed = true;
        _timer = revealDuration;
        
        Debug.Log($"{gameObject.name} が表示されました。タイマーリセット。");
    }

    private void UpdateRevealTimer()
    {
        // 表示中でない、または一時停止中なら何もしない
        if (!isRevealed) return;

        // タイマーを減らす
        _timer -= Time.deltaTime;

        // 0になったら非表示に戻す
        if (_timer <= 0)
        {
            isRevealed = false;
            _timer = 0;
            Debug.Log($"{gameObject.name} が隠れました。");
        }
    }

    // public void SpawnUnitDelayed(UnitData unitData)
    // {
    //     if (unitObject != null) return;

    //     // 呼び出し中の仮ユニットの作成
    //     Vector3 tilePosition = gameObject.transform.position;
    //     Vector3 unitPosition = new Vector3(tilePosition.x, 0.75f, tilePosition.z);
    //     Quaternion unitRotation = owner == TileOwner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
    //     unitObject = Instantiate(unitData.callingPrefab, unitPosition, unitRotation, gameObject.transform);
    //     // マップデータの更新を促す
    //     mapManager.isDirty = true;

    //     // 呼び出し完了時の処理
    //     Action onCompleteCallback = async () =>
    //     {
    //         // 仮ユニットの除去
    //         Destroy(unitObject);
    //         while (unitObject != null) {
    //             await Task.Yield();
    //         }
    //         // 本命ユニットの作成
    //         SpawnUnit(unitData);
    //     };
    //     // 仮ユニットの初期化処理
    //     unitStats.Initialize(unitData.callingProfile);
    //     callingUnitController.StartTimer(unitData.callTime, onCompleteCallback);
    // }

    // public void SpawnUnit(UnitData unitData)
    // {
    //     if (unitObject != null) return;

    //     Vector3 tilePosition = gameObject.transform.position;
    //     Vector3 unitPosition = new Vector3(tilePosition.x, unitData.initPos.y, tilePosition.z);
    //     Quaternion unitRotation = owner == TileOwner.Enemy ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
    //     unitObject = Instantiate(unitData.prefab, unitPosition, unitRotation, gameObject.transform);
    //     unitStats.Initialize(unitData.profile);

    //     // マップデータの更新を促す
    //     mapManager.isDirty = true;

    //     Debug.Log("本オブジェクト配置完了");
    // }

    public async void DestroyUnit()
    {
        Destroy(unitObject);
        while (unitObject != null) {
            await Task.Yield();
        }
        // マップデータの更新を促す
        mapManager.isDirty = true;
    }
}
