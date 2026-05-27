using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UnitAnimation : MonoBehaviour
{
    public AnimationName currentAnimation;

    // private bool isAnimating = false;
    public bool isBusyAnimating = false;
    public bool isPause = false;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Play(AnimationName stateName)
    {
        _animator.Play(stateName);
    }

    public void Resume()
    {
        _animator.speed = 1;
        isPause = false;
    }

    public void Pause()
    {
        _animator.speed = 0;
        isPause = true;
    }

    public void PlayOnce(string stateName)
    {
        PlayOnceAsync(stateName).Forget();
    }

    public async UniTask PlayOnceAsync(string stateName)
    {
        if (isBusyAnimating) return;

        isBusyAnimating = true;

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
            isBusyAnimating = false;
        }
    }
}
