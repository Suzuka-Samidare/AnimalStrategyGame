using System.Collections.Generic;
using UnityEngine;

public class FactionUnitPool : MonoBehaviour
{
    [System.Serializable]
    public struct PoolSetupData
    {
        public UnitType Type;
        public UnitBase Prefab;
        public int DefaultCapacity;
        public int MaxSize;
    }

    [SerializeField] private List<PoolSetupData> setupDataList;
    
    private Dictionary<UnitType, ComponentPool<UnitBase>> _poolDictionary = new();

    private void Awake()
    {
        foreach (var data in setupDataList)
        {
            // 子オブジェクトとして純粋なプールを動的に生成（ヒエラルキーも整理されて綺麗！）
            GameObject poolObj = new GameObject($"{data.Type}Pool");
            poolObj.transform.SetParent(this.transform);
            
            // C#クラスとしてプールを new する（AddComponent は不要に！）
            var unitPool = new ComponentPool<UnitBase>(
                prefab: data.Prefab,
                parent: poolObj.transform,
                defaultCapacity: data.DefaultCapacity > 0 ? data.DefaultCapacity : 5,
                maxSize: data.MaxSize > 0 ? data.MaxSize : 10
            );

            _poolDictionary[data.Type] = unitPool;
        }
    }

    public UnitBase Spawn(UnitType type, Vector3 position, Quaternion rotation)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            UnitBase unit = pool.Get();
            unit.transform.SetPositionAndRotation(position, rotation);
            return unit;
        }
        Debug.LogWarning($"プールに {type} が登録されてないよ！");
        return null;
    }

    public void Despawn(UnitType type, UnitBase unit)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            pool.Release(unit);
        }
    }
}