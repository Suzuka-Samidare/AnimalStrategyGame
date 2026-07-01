using UnityEngine;

public abstract class AttackerStatsBase : UnitStatsBase
{
    [Tooltip("攻撃ステータス")] public AttackProfile attackProfile;

    protected override void Initialize(UnitData unitData)
    {
        base.Initialize(unitData);
        
        // unitDataがAttackUnitDataであればプロフィールを設定
        if (unitData is AttackUnitData attackUnitData)
        {
            attackProfile = attackUnitData.attackProfile;
        }
        else
        {
            throw new System.Exception("AttackUnitDataのデータではありません");
        }
    }
}
