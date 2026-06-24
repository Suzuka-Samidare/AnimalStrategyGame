using UnityEngine;

[RequireComponent(typeof(DefenceUnitStats))]
[RequireComponent(typeof(DefenceUnitController))]
public class DefenceUnit : BaseUnit, IDefendable
{
    [Header("Refs")]
    // [SerializeField] private UnitStats baseStats;
    // [SerializeField] private UnitController baseController;
    [SerializeField] private DefenceUnitStats defenceStats;
    [SerializeField] private DefenceUnitController defenceController;

    // public UnitStats BaseStats => baseStats;
    // public UnitController BaseController => baseController;
    // public DefenceUnitStats DefenceStats => defenceStats;
    // public DefenceUnitController DefenceController => defenceController;

    // UnitStats IUnit.Stats => baseStats;
    // UnitController IUnit.Controller => baseController;
    DefenceUnitStats IDefendable.Stats => defenceStats;
    DefenceUnitController IDefendable.Controller => defenceController;
}