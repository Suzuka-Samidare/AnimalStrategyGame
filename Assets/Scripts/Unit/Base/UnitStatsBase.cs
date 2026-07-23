using System.Diagnostics;
using UnityEngine;
using Guid = System.Guid;

public abstract class UnitStatsBase : MonoBehaviour
{
    [Header("静的ステータス")]
    [Tooltip("UID")] private string _uuid;
    [Tooltip("基本ステータス")] public UnitProfile profile;
    [Tooltip("オーナー情報")] public Owner Owner;
    

    [Header("動的ステータス")]
    [Tooltip("気絶フラグ")] public bool IsFaint = false;
    [Tooltip("耐久値"), SerializeField]
    private float _hp;
    public float hp
    {
        get => _hp;
        protected set => _hp = value;
    }
    [Tooltip("敵から視認可能か"), SerializeField]
    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
        }
    }

    protected virtual void Awake()
    {
        _uuid = Guid.NewGuid().ToString();
    }

    public virtual void Initialize(UnitData unitData)
    {
        // 基本ステータスの初期化
        this.profile = unitData.profile;
        hp = profile.maxHp;
    }

    /// <summary>
    /// HP更新（ダメージ、回復）
    /// </summary>
    private void UpdateHp(float amount)
    {
        // HPの増減計算
        hp = Mathf.Clamp(hp + amount, 0, profile.maxHp);
    }

    /// <summary>
    /// ダメージ反映
    /// </summary>
    public void ApplyDamageAsync(float power, Tile tile)
    {
        // HP更新
        UpdateHp(-power);

        // HPが0以下の場合、気絶フラグを立てる
        if (hp <= 0) IsFaint = true;
    }
}
