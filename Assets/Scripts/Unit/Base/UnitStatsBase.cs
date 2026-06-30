using Cysharp.Threading.Tasks;
using UnityEngine;
using Guid = System.Guid;

public abstract class UnitStatsBase : MonoBehaviour
{
    [Header("静的ステータス")]
    [Tooltip("UID")] private string _uuid;
    [Tooltip("基本ステータス")] public UnitProfile profile;

    [Header("動的ステータス")]
    [Tooltip("気絶フラグ")] public bool isFaint = false;
    [Tooltip("耐久値")] public float hp { get; protected set; }

    protected virtual void Awake()
    {
        _uuid = Guid.NewGuid().ToString();
    }

    protected void Initialize(UnitProfile profile)
    {
        // 基本ステータスの初期化
        this.profile = profile;
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
        if (hp <= 0) isFaint = true;
    }
}
