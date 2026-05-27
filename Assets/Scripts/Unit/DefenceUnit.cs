using UnityEngine;

public class DefenceUnit : MonoBehaviour, IUnit, IDefendable
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;
    [SerializeField] private DefenceUnitStats defenceStats;
    // [SerializeField] private DefenceUnitController defenceController;

    public UnitStats BaseStats => baseStats;
    public UnitController BaseController => baseController;
    public DefenceUnitStats DefenceStats => defenceStats;
    // public DefenceUnitController DefenceController => defenceController;
}