using UnityEngine;

public class BaseUnit : MonoBehaviour, IUnit
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;

    public UnitStats BaseStats => baseStats;
    public UnitController BaseController => baseController;
}
