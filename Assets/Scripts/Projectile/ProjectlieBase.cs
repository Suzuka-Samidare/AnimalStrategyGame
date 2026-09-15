using UnityEngine;

public class ProjectlieBase : MonoBehaviour
{
    [Header("静的ステータス")]
    [Tooltip("基本ステータス")] public ProjectileType projectileType;

    /// <summary>
    /// 発射物の初期化処理
    /// </summary>
    public virtual void Setup(ProjectileType type)
    {
        projectileType = type;
    }
}
