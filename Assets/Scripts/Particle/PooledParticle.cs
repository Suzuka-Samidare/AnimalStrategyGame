using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private ComponentPool<PooledParticle> _pool;

    // 再生完了を通知するためのソース
    private AutoResetUniTaskCompletionSource _utcs;

    private void Awake()
    {
        // パーティクルオブジェクトにあるコンポーネントを取得
        _particleSystem = GetComponent<ParticleSystem>();
        
        // 「Stop Action」を「Callback」にしておく
        // パーティクルの再生終了時にOnParticleSystemStoppedを実行されるようになる。
        var main = _particleSystem.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    // プールをセットする用
    public void Initialize(ComponentPool<PooledParticle> pool)
    {
        _pool = pool;
    }

    // クリーンに再生する処理（子要素も巻き込む）
    public UniTask PlayEffectAsync(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        
        // 前回の残骸をクリア
        _particleSystem.Clear(withChildren: true);
        // 再利用可能なCompletionSourceを作成
        _utcs = AutoResetUniTaskCompletionSource.Create();
        // 再生（withChildren: trueで子要素も同様の操作を実行）
        _particleSystem.Play(withChildren: true);

        return _utcs.Task;
    }

    // パーティクルの再生終了時にUnityから呼出されるコールバック
    private void OnParticleSystemStopped()
    {
        // 待機している処理があれば完了を通知
        if (_utcs != null)
        {
            var temp = _utcs;
            _utcs = null;
            temp.TrySetResult(); // ここで await していた場所にシグナルが飛ぶ
        }

        // プールへ返却
        if (_pool != null)
        {
            _pool.Release(this);
        }
    }

    // 万が一再生中にオブジェクト自体が破棄された場合のリーク防止策
    private void OnDestroy()
    {
        if (_utcs != null)
        {
            _utcs.TrySetCanceled();
        }
    }
}