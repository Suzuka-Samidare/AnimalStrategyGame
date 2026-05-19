using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private IObjectPool<PooledParticle> _pool;

    private void Awake()
    {
        // パーティクルオブジェクトにあるコンポーネントを取得
        _particleSystem = GetComponent<ParticleSystem>();
        
        // インスペクターで「Stop Action」を「None」にしておくのがポイント！
        // スクリプト側で終了を検知してプールに戻すよ
        var main = _particleSystem.main;
        main.stopAction = ParticleSystemStopAction.None;
    }

    // プールをセットする用
    public void SetPool(IObjectPool<PooledParticle> pool)
    {
        _pool = pool;
    }

    // パーティクルが終了したらプールに戻す判定
    private void OnParticleSystemStopped()
    {
        // 安全にプールに返却
        _pool?.Release(this);
    }
}