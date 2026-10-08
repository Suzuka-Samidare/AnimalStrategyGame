// ジェネリクスを持たない、ただの抽象クラスを作る
using UnityEngine;
using System.Collections.Generic;

public abstract class AttackerUnitBase : UnitBase
{
    // アタッカー共通のステータスとコントローラーの窓口を用意
    public new AttackerStatsBase Stats => base.Stats as AttackerStatsBase;
    public new AttackerControllerBase Controller => base.Controller as AttackerControllerBase;

    public void EnableAttackSchedule()
    {
        if (Stats.IsAttackScheduled) return;
        Stats.IsAttackScheduled = true;
        Animation.Play(AnimationName.Sit);
    }

    public void DisableAttackSchedule()
    {
        if (!Stats.IsAttackScheduled) return;
        Stats.IsAttackScheduled = false;
        if (gameObject.activeSelf)
        {
            Animation.Play(AnimationName.IdleA);
        }
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
