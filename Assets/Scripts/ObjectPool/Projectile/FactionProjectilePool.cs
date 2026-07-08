using System.Collections.Generic;
using UnityEngine;

public enum ProjectileType
{
    Ink,
    HerringSchool,
}

public class FactionProjectilePool : MonoBehaviour
{
    [System.Serializable]
    public struct PoolSetupData
    {
        public ProjectileType Type;
        public ProjectlieBase Prefab;
        public int DefaultCapacity;
        public int MaxSize;
    }

    [SerializeField] private List<PoolSetupData> setupDataList;
    
    private Dictionary<ProjectileType, ComponentPool<ProjectlieBase>> _poolDictionary = new();

    private void Awake()
    {
        foreach (var data in setupDataList)
        {
            // 子オブジェクトとして純粋なプールを動的に生成（ヒエラルキーも整理されて綺麗！）
            GameObject poolObj = new GameObject($"{data.Type}Pool");
            poolObj.transform.SetParent(this.transform);
            
            // C#クラスとしてプールを new する（AddComponent は不要に！）
            var unitPool = new ComponentPool<ProjectlieBase>(
                prefab: data.Prefab,
                parent: poolObj.transform,
                defaultCapacity: data.DefaultCapacity > 0 ? data.DefaultCapacity : 5,
                maxSize: data.MaxSize > 0 ? data.MaxSize : 10
            );

            _poolDictionary[data.Type] = unitPool;
        }
    }

    public ProjectlieBase Spawn(ProjectileType type, Vector3 position, Quaternion rotation)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            ProjectlieBase projectile = pool.Get();
            projectile.transform.SetPositionAndRotation(position, rotation);
            return projectile;
        }
        Debug.LogWarning($"プールに {type} が登録されてないよ！");
        return null;
    }

    public void Despawn(ProjectileType type, ProjectlieBase projectlie)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            pool.Release(projectlie);
        }
    }
}