using UnityEngine;

public class AttackUnit : MonoBehaviour, IUnit, IAttackable
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;
    [SerializeField] private AttackUnitStats attackStats;
    [SerializeField] private AttackUnitController attackController;

    public UnitStats BaseStats => baseStats;
    public UnitController BaseController => baseController;
    public AttackUnitStats AttackStats => attackStats;
    public AttackUnitController AttackController => attackController;
}