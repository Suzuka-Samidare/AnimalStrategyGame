using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [Header("オブジェクトプール本体")]
    [SerializeField] private FactionProjectilePool _pool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public ProjectlieBase SpawnProjectile(ProjectileType type, Vector3 position, Quaternion rotation)
    {
        // FactionProjectilePool targetPool;
        // switch (type)
        // {
        //     case ProjectileType.Ink:
        //         targetPool = _inkPool;
        //         break;
        //     default:
        //         throw new System.Exception("対応するプールがありません");
        // }

        return _pool.Spawn(type, position, rotation);
    }

    public void DespawnProjectile(ProjectlieBase projectile)
    {
        _pool.Despawn(projectile.Type, projectile);
    }
}
