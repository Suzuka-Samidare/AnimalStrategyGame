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

        // 開発中にアタッチ忘れを即座に気づかせる
        Debug.Assert(Stats != null, $"{gameObject.name} に UnitStatsBase がアタッチされていません。", this);
        Debug.Assert(Controller != null, $"{gameObject.name} に ControllerBase がアタッチされていません。", this);
    }

    /// <summary>
    /// ユニット内コンポーネントの初期化処理
    /// </summary>
    public virtual void Setup(Owner owner, UnitData unitData)
    {
        Stats.Initialize(unitData);
        Stats.Owner = owner;

        Controller.Initialize(this);

        SetVisible(false);
    }

    /// <summary>
    /// 可視状態の更新
    /// </summary>
    public void SetVisible(bool value)
    {
        Stats.IsVisible = value;
        Controller.UpdateVisibility();
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