using UnityEngine;

[RequireComponent(typeof(UnitStats))]
[RequireComponent(typeof(UnitController))]
public class BaseUnit : MonoBehaviour, IUnit
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;

    // public UnitStats BaseStats => baseStats;
    // public UnitController BaseController => baseController;

    UnitStats IUnit.Stats => baseStats;
    UnitController IUnit.Controller => baseController;

    private void Awake()
    {
        if (baseStats == null) throw new System.Exception("UnitStatsがアタッチされていません");
        if (baseController == null) throw new System.Exception("UnitControllerがアタッチされていません");
    }
}
