using UnityEngine;

public class SupportUnit : MonoBehaviour, IUnit, ISupportable
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;
    [SerializeField] private SupportUnitStats supportStats;
    // [SerializeField] private SupportUnitController supportController;

    public UnitStats BaseStats => baseStats;
    public UnitController BaseController => baseController;
    public SupportUnitStats SupportStats => supportStats;
    // public SupportUnitController SupportController => supportController;
}