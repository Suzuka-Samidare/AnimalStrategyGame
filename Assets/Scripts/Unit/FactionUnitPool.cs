using System.Collections.Generic;
using UnityEngine;

public class FactionUnitPool : MonoBehaviour
{
    [System.Serializable]
    public struct PoolSetupData
    {
        public UnitType type;
        public GameObject prefab;
    }

    [SerializeField] private List<PoolSetupData> setupDataList;
    
    private Dictionary<UnitType, UnitPool> _poolDictionary = new();

    private void Awake()
    {
        foreach (var data in setupDataList)
        {
            // 子オブジェクトとして純粋なプールを動的に生成（ヒエラルキーも整理されて綺麗！）
            GameObject poolObj = new GameObject($"{data.type}Pool");
            poolObj.transform.SetParent(this.transform);
            
            UnitPool unitPool = poolObj.AddComponent<UnitPool>();
            unitPool.Initialize(data.prefab);

            _poolDictionary[data.type] = unitPool;
        }
    }

    public GameObject Spawn(UnitType type, Vector3 position, Quaternion rotation)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            GameObject obj = pool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }
        Debug.LogWarning($"プールに {type} が登録されてないよ！");
        return null;
    }

    public void Despawn(UnitType type, GameObject obj)
    {
        if (_poolDictionary.TryGetValue(type, out var pool))
        {
            pool.Release(obj);
        }
    }
}