// ジェネリクスを持たない、ただの抽象クラスを作る
public abstract class AttackerUnitBase : UnitBase
{
    // アタッカー共通のステータスとコントローラーの窓口を用意
    public new AttackerStatsBase Stats => base.Stats as AttackerStatsBase;
    public new AttackerControllerBase Controller => base.Controller as AttackerControllerBase;

    public void NotifyAttackScheduled()
    {
        if (Stats.IsAttackScheduled) return;
        Stats.IsAttackScheduled = true;
        Animation.Play(AnimationName.Sit);
    }

    public void NotifyAttackScheduleCleared()
    {
        if (!Stats.IsAttackScheduled) return;
        Stats.IsAttackScheduled = false;
    }
}

// 既存のジェネリクス版は、上記を継承するように書き換える
public abstract class AttackerUnitBase<TStats, TController> : AttackerUnitBase
    where TStats : AttackerStatsBase 
    where TController : AttackerControllerBase
{
    public new TStats Stats => base.Stats as TStats;
    public new TController Controller => base.Controller as TController;
}
