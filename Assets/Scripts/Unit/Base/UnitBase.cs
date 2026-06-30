using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    // 外部から「.Stats」「.Controller」「.Animation」でアクセスするためのプロパティ
    // ※ 派生クラス（SquidやHerring）がそれぞれの具象コンポーネントを返すように抽象化します
    public abstract UnitStatsBase Stats { get; }
    public abstract UnitControllerBase Controller { get; }
    public abstract UnitAnimationBase Animation { get; }

    // ==========================================
    // ★ 全unitで共通して利用できる処理（共通メソッド）
    // ==========================================
    protected virtual void Start()
    {
        if (Controller != null)
        {
            Controller.Initialize(this);
        }
    }

    public virtual void PlaySpawnEffect()
    {
        // 全ユニット共通の登場エフェクト処理など
    }
}