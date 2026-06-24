using UnityEngine;

[RequireComponent(typeof(AttackUnitStats))]
[RequireComponent(typeof(AttackUnitController))]
public class AttackUnit : BaseUnit, IAttackable
{
    [Header("Refs")]
    // [SerializeField] private UnitStats baseStats;
    // [SerializeField] private UnitController baseController;
    [SerializeField] private AttackUnitStats attackStats;
    [SerializeField] private AttackUnitController attackController;

    // public UnitStats BaseStats => baseStats;
    // public UnitController BaseController => baseController;
    // public AttackUnitStats AttackStats => attackStats;
    // public AttackUnitController AttackController => attackController;

    // UnitStats IUnit.Stats => baseStats;
    // UnitController IUnit.Controller => baseController;
    AttackUnitStats IAttackable.Stats => attackStats;
    AttackUnitController IAttackable.Controller => attackController;

    private void Awake()
    {
        // if (baseStats == null) throw new System.Exception("UnitStatsがアタッチされていません");
        // if (baseController == null) throw new System.Exception("UnitControllerがアタッチされていません");
        if (attackStats == null) throw new System.Exception("AttackUnitStatsがアタッチされていません");
        if (attackController == null) throw new System.Exception("AttackUnitControllerがアタッチされていません");
    }
}