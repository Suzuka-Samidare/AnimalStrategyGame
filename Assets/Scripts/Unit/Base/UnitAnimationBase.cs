using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UnitAnimationBase : MonoBehaviour
{
    private bool isAnimating = false;
    private bool isPause = false;

    private Animator _animator;

    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public virtual void Play(AnimationName stateName)
    {
        _animator.Play(stateName);
    }

    public virtual void Resume()
    {
        _animator.speed = 1;
        isPause = false;
    }

    public virtual void Pause()
    {
        _animator.speed = 0;
        isPause = true;
    }

    public virtual void PlayOnce(AnimationName stateName)
    {
        PlayOnceAsync(stateName).Forget();
    }

    public async UniTask PlayOnceAsync(AnimationName stateName)
    {
        if (isAnimating) return;

        isAnimating = true;

        try
        {
            // アニメーションを再生
            _animator.Play(stateName, 0, 0f);
            //  1フレーム待機（Animatorの更新を待たないと、前のステート情報が取れてしまう）
            await UniTask.Yield();
            // アニメーションが終わるまで待機
            await UniTask.WaitUntil(() =>
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                return stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1.0f;
            }, cancellationToken: this.GetCancellationTokenOnDestroy());
            // 元のアニメーションに戻す
            _animator.Play(AnimationName.IdleA);
        }
        finally
        {
            isAnimating = false;
        }
    }
}
