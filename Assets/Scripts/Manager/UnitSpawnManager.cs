using UnityEngine;

public enum FactionType { Player, Enemy }

public class UnitSpawnManager : MonoBehaviour
{
    [SerializeField] private FactionUnitPool playerPool;
    [SerializeField] private FactionUnitPool enemyPool;

    // 🌟 これを呼ぶだけで、適切な陣営のプールからユニットが出る！
    public GameObject SpawnUnit(FactionType faction, UnitType unitType, Vector3 position)
    {
        FactionUnitPool targetPool = (faction == FactionType.Player) ? playerPool : enemyPool;
        GameObject unit = targetPool.Spawn(unitType, position, Quaternion.identity);

        // ここで「君はプレイヤー側だよ」「敵側だよ」というコンポーネントの初期化を渡すとスマート！
        // unit.GetComponent<UnitController>().Setup(faction);

        return unit;
    }

    public void DespawnUnit(FactionType faction, UnitType unitType, GameObject unit)
    {
        FactionUnitPool targetPool = (faction == FactionType.Player) ? playerPool : enemyPool;
        targetPool.Despawn(unitType, unit);
    }
}