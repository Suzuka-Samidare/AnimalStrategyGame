using UnityEngine;

public abstract class UnitBase<TStats, TController> : UnitBase 
    where TStats : UnitStatsBase 
    where TController : UnitControllerBase
{
    // 💡 内部に、あらかじめ正しい型でキャストされたプロパティを用意しておく！
    public new TStats Stats => base.Stats as TStats;
    public new TController Controller => base.Controller as TController;
}
