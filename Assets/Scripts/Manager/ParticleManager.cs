using UnityEngine;
using Cysharp.Threading.Tasks;
using ParticleType = FactionParticlePool.ParticleType;

public class ParticleManager : MonoBehaviour, IInitializable
{
    public static ParticleManager Instance { get; private set; }

    [Header("オブジェクトプール本体")]
    [SerializeField] private FactionParticlePool _pool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
  
    public async UniTask Initialize()
    {
        await UniTask.CompletedTask;
    }

    public async UniTask PerformFireExplosionAsync(Vector3 position, Quaternion rotation)
    {
        await _pool.SpawnAsync(ParticleType.FireExplosion, position, rotation);
    }
}
