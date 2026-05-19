using UnityEngine;
using UnityEngine.Pool; // これを使う！
using Cysharp.Threading.Tasks;

public class ParticlePoolManager : MonoBehaviour, IInitializable
{
    public static ParticlePoolManager Instance { get; private set; }

    [Header("オブジェクトプール本体")]
    private IObjectPool<PooledParticle> _pool;

    [Header("プール設定")]
    [SerializeField] private PooledParticle _prefab;
    [SerializeField] private bool _collectionCheck = true;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 12;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        _pool = new ObjectPool<PooledParticle>(
            OnCreatePooledItem, //第1関数：プールにオブジェクトがない場合オブジェクト生成(Instantiate)する
            OnTakeFromPool, //第2関数：プールに使用していないオブジェクトがある場合はプールから出す。SetActive(true)する
            OnReturnedToPool, //第3関数：プールに返却する
            OnDestroyPoolObject, //第4関数：プールの許容量を超えた時にオブジェクトを削除する
            collectionCheck: _collectionCheck, //既にプールにあるオブジェトを追加した場合に例外とするか。エディタでのみ実行される
            defaultCapacity: _defaultCapacity, //初期のプールサイズ
            maxSize: _maxSize //最大プールサイズ
        );
    }
  
    public async UniTask Initialize()
    {
        PreWarmPool();
        await UniTask.CompletedTask;
    }

    private void PreWarmPool()
    {
        PooledParticle[] tempArray = new PooledParticle[_defaultCapacity];

        // いったん全部Getして生成させる
        for (int i = 0; i < _defaultCapacity; i++)
        {
            tempArray[i] = _pool.Get();
        }

        // すぐにプールに戻すことで「未使用のストック」にする！
        for (int i = 0; i < _defaultCapacity; i++)
        {
            _pool.Release(tempArray[i]);
        }
    }

    // 1. 新しくインスタンスを作る
    private PooledParticle OnCreatePooledItem()
    {
        var instance = Instantiate(_prefab, transform);
        instance.SetPool(_pool);
        return instance;
    }

    // 2. プールから引っ張ってきてアクティブにする
    private void OnTakeFromPool(PooledParticle particle)
    {
        particle.gameObject.SetActive(true);
    }

    // 3. 使い終わったら非アクティブにしてプールに眠らせる
    private void OnReturnedToPool(PooledParticle particle)
    {
        particle.gameObject.SetActive(false);
    }

    // 4. プールの上限を超えて要らなくなった分は破棄
    private void OnDestroyPoolObject(PooledParticle particle)
    {
        Destroy(particle.gameObject);
    }

    // 外部から呼び出す用のカスタムEmitメソッド
    public PooledParticle SpawnParticle(Vector3 position, Quaternion rotation)
    {
        var particle = _pool.Get(); // プールから取得（なければ自動生成される）
        particle.transform.SetPositionAndRotation(position, rotation);
        return particle;
    }
}
