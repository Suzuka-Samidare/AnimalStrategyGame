using UnityEngine;
using UnityEngine.Pool;

// 汎用プール
public class ComponentPool<T> where T : MonoBehaviour
{
    private T _prefab;
    private Transform _parent;
    private ObjectPool<T> _pool;

    public ComponentPool(T prefab, Transform parent, int defaultCapacity = 5, int maxSize = 10)
    {
        _prefab = prefab;
        _parent = parent;
        
        _pool = new ObjectPool<T>(
            createFunc: () => Object.Instantiate(_prefab, _parent),
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => { if (obj != null) Object.Destroy(obj.gameObject); },
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    public T Get() => _pool.Get();
    public void Release(T obj) => _pool.Release(obj);
    public void Clear() => _pool.Clear();
}