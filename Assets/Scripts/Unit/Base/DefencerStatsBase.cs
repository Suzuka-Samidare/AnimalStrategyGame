using UnityEngine;

public abstract class DefencerStatsBase : UnitStatsBase
{   
    [Tooltip("防衛ステータス")] public DefenceProfile defenceProfile;
    // TODO: DefenceProfileに入れることを考える
    [Tooltip("奥行の守備距離")] public int VerticalRange = 5;

    protected override void Initialize(UnitData unitData)
    {
        base.Initialize(unitData);
        
        // unitDataがAttackUnitDataであればプロフィールを設定
        if (unitData is DefenceUnitData defenceUnitData)
        {
            defenceProfile = defenceUnitData.defenceProfile;
        }
        else
        {
            throw new System.Exception("DefenceUnitDataのデータではありません");
        }
    }
}
