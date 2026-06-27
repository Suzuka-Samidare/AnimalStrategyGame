using UnityEngine;
using UnityEngine.Pool;

public class UnitPool : MonoBehaviour
{
    private GameObject _prefab;
    private ObjectPool<GameObject> _pool;

    public void Initialize(GameObject prefab, int defaultCapacity = 5, int maxSize = 10)
    {
        _prefab = prefab;
        _pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_prefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    public GameObject Get() => _pool.Get();
    public void Release(GameObject obj) => _pool.Release(obj);
}