using UnityEngine;

public class CallingUnit : MonoBehaviour, IUnit, ICallable
{
    [Header("Refs")]
    [SerializeField] private UnitStats baseStats;
    [SerializeField] private UnitController baseController;
    [SerializeField] private CallingUnitController callingController;

    // public UnitStats BaseStats => baseStats;
    // public UnitController BaseController => baseController;
    // public CallingUnitController CallingController => callingController;

    UnitStats IUnit.Stats => baseStats;
    UnitController IUnit.Controller => baseController;
    CallingUnitController ICallable.Controller => callingController;

    private void Awake()
    {
        if (baseStats == null) throw new System.Exception("UnitStatsがアタッチされていません");
        if (baseController == null) throw new System.Exception("UnitControllerがアタッチされていません");
        if (callingController == null) throw new System.Exception("CallingUnitControllerがアタッチされていません");
    }
}
