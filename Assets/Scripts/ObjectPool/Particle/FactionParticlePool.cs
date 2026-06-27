using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class FactionParticlePool : MonoBehaviour
{
    public enum ParticleType
    {
        FireExplosion,
        InkExplosion,
    }

    [System.Serializable]
    public struct PoolSetupData
    {
        public ParticleType Type;
        public PooledParticle Prefab;
        public int DefaultCapacity;
        public int MaxSize;
    }

    [SerializeField] private List<PoolSetupData> setupDataList;
    
    private Dictionary<ParticleType, ComponentPool<PooledParticle>> _poolDictionary = new();

    private void Awake()
    {
        foreach (var data in setupDataList)
        {
            // 子オブジェクトとして純粋なプールを動的に生成（ヒエラルキーも整理されて綺麗！）
            GameObject poolObj = new GameObject($"{data.Type}Pool");
            poolObj.transform.SetParent(this.transform);
            
            // C#クラスとしてプールを new する（AddComponent は不要に！）
            var particlePool = new ComponentPool<PooledParticle>(
                prefab: data.Prefab,
                parent: poolObj.transform,
                defaultCapacity: data.DefaultCapacity > 0 ? data.DefaultCapacity : 5,
                maxSize: data.MaxSize > 0 ? data.MaxSize : 10
            );

            _poolDictionary[data.Type] = particlePool;
        }
    }

    public void Spawn(ParticleType type, Vector3 position, Quaternion rotation)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            PooledParticle particle = pool.Get();
            // 再生完了時に自身をリリースさせるために、管理しているプールを共有
            particle.Initialize(pool);
            // 再生開始
            particle.PlayEffectAsync(position, rotation).Forget();

        }
        else
        {    
            Debug.LogWarning($"プールに {type} が登録されてないよ！");
        }
    }

    public async UniTask SpawnAsync(ParticleType type, Vector3 position, Quaternion rotation)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            PooledParticle particle = pool.Get();
            // 再生完了時に自身をリリースさせるために、管理しているプールを共有
            particle.Initialize(pool);
            // 再生開始
            await particle.PlayEffectAsync(position, rotation);
        }
        else
        {    
            Debug.LogWarning($"プールに {type} が登録されてないよ！");
        }
    }
}
