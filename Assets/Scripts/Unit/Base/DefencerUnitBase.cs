// ① ジェネリクスを持たない、ただの抽象クラスを作る
public abstract class DefencerUnitBase : UnitBase
{
    // アタッカー共通のステータスとコントローラーの窓口を用意
    public new DefencerStatsBase Stats => base.Stats as DefencerStatsBase;
    public new DefencerControllerBase Controller => base.Controller as DefencerControllerBase;
}

// ② 既存のジェネリクス版は、上記を継承するように書き換える
public abstract class DefencerUnitBase<TStats, TController> : DefencerUnitBase
    where TStats : DefencerStatsBase 
    where TController : DefencerControllerBase
{
    public new TStats Stats => base.Stats as TStats;
    public new TController Controller => base.Controller as TController;
}