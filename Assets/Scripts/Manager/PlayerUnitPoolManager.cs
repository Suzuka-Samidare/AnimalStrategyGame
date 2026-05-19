using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool; // これを使う！

public class PlayerUnitPoolManager : MonoBehaviour, IInitializable
{
    [System.Serializable]
    public struct UnitPrefabConfig
    {
        public UnitType type;
        public GameObject prefab;
    }
    public List<UnitPrefabConfig> prefabConfigs;
    
    // 種類ごとのプールを辞書で管理
    private Dictionary<UnitType, ObjectPool<GameObject>> _pools 
        = new Dictionary<UnitType, ObjectPool<GameObject>>();

    private void Awake()
    {
        foreach (var config in prefabConfigs)
        {
            // ここで種類ごとにプールを初期化！
            var type = config.type;
            var prefab = config.prefab;

            var pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab), // 足りない時に生成する処理
                actionOnGet: (obj) => obj.SetActive(true), // プールから出す時の処理
                actionOnRelease: (obj) => obj.SetActive(false), // プールに戻す時の処理
                actionOnDestroy: (obj) => Destroy(obj), // プールが溢れた時の処理
                defaultCapacity: 10, // 最初に用意する目安
                maxSize: 10 // プールの最大保持数
            );

            _pools.Add(type, pool);
        }
    }

    public async UniTask Initialize()
    {
        await UniTask.CompletedTask;
    }

    // ユニットを呼び出すときはこれを使う
    public GameObject GetUnit(UnitType type, Vector3 position, Quaternion rotation)
    {
        if (_pools.TryGetValue(type, out var pool))
        {
            GameObject obj = pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        return null;
    }

    // ユニットが死んだらこれでプールに戻す
    public void ReleaseUnit(UnitType type, GameObject unitInstance)
    {
        if (_pools.TryGetValue(type, out var pool))
        {
            pool.Release(unitInstance);
        }
    }
}
