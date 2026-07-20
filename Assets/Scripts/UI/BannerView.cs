using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;

public class BannerView : MonoBehaviour
{
    private TextMeshProUGUI _bannerText;
    private Animator _animator;
    private VisibilityController _visibility;

    void Awake()
    {
        _bannerText = GetComponentInChildren<TextMeshProUGUI>();
        _animator = GetComponent<Animator>();
        _visibility = GetComponent<VisibilityController>();
        _visibility.Show();

        if (_bannerText == null || _animator == null || _visibility == null)
        {
            throw new Exception("バナーパーツの初期化処理に失敗しました。");
        }
    }

    public async UniTask PlayAnnouncement(string text)
    {
        _bannerText.text = text;

        _visibility.Show();
        _animator.SetTrigger("Play");

        await UniTask.WaitUntil(() => IsAnimationFinished("Close"));

        _animator.Rebind();
        _animator.Update(0f);
    }

    public async UniTask PlayOpenAnimationAsync(string text)
    {
        // テキストの更新
        _bannerText.text = text;
        // Playトリガーの発火
        _animator.SetTrigger("Open");
        // Openステートの完了を待つ
        await UniTask.WaitUntil(() => IsAnimationFinished("Banner_Open"));
    }

    public async UniTask PlayCloseAnimationAsync()
    {
        // Closeトリガーの発火
        _animator.SetTrigger("Close");
        // Closeステートの完了を待つ
        await UniTask.WaitUntil(() => IsAnimationFinished("Banner_Close"));
        // 初期化処理
        _animator.Rebind();
        _animator.Update(0f);
    }

    private bool IsAnimationFinished(string stateName)
    {
        // 現在のステート情報を取得（0はBase Layer）
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // 1. 指定したステート名であること
        // 2. normalizedTime（再生率）が 1.0（100%）を超えていること
        // 3. 遷移中（Transition）ではないこと
        return stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1.0f && !_animator.IsInTransition(0);
    }
}
