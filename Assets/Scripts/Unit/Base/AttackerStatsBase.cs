using UnityEngine;

public abstract class AttackerStatsBase : UnitStatsBase
{
    [Tooltip("攻撃ステータス")]
    public AttackProfile attackProfile;
    [Tooltip("攻撃")]
    private bool _isAttackScheduled;
    public bool IsAttackScheduled
    {
        get => _isAttackScheduled;
        set
        {
            if (_isAttackScheduled == value) return;
            _isAttackScheduled = value;
        }
    }

    public override void Initialize(UnitData unitData)
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
