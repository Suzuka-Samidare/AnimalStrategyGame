using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    // 外部から「.Stats」「.Controller」「.Animation」でアクセスするためのプロパティ
    // ※ 派生クラス（SquidやHerring）がそれぞれの具象コンポーネントを返すように抽象化します
    public virtual UnitStatsBase Stats { get; protected set; }
    public virtual UnitControllerBase Controller { get; protected set; }
    public virtual UnitAnimationBase Animation { get; protected set; }

    protected virtual void Awake()
    {
        Stats = GetComponent<UnitStatsBase>();
        Controller = GetComponent<UnitControllerBase>();
        Animation = GetComponent<UnitAnimationBase>();
    }

    public virtual void Setup(UnitData unitData)
    {
        if (Stats != null) Stats.Initialize(unitData);
        if (Controller != null) Controller.Initialize(this);
    }

    /// <summary>
    /// 気絶処理
    /// </summary>
    public async UniTask OnFaint(Tile tile)
    {
        // もし気絶アニメーションがあれば、再生する
        if (Animation)
        {
            await Animation.PlayOnceAsync(AnimationName.Death);
        }
        // デスポーン処理
        UnitSpawnManager.Instance.DespawnUnit(tile);
    }
}