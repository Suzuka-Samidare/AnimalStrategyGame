using System;
using System.Collections.Generic;
using UnityEngine;
using Phase = GameManager.Phase;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Serializable]
    private struct BgmLoopInfo
    {
        public Phase section;
        public int loopStartSample;
        public int loopHalfSample;
        public int loopEndSample; 
    }

    [Header("BGM情報")]
    [SerializeField] private AudioClip _mainBgm;
    [SerializeField] private List<BgmLoopInfo> _loopInfoList = new List<BgmLoopInfo>(4);
    private Dictionary<Phase, BgmLoopInfo> _loopInfoDict;

    [Header("現在のループ情報")]
    [SerializeField] private BgmLoopInfo _currentBgmLoopInfo;

    [Header("次回のループ情報")]
    [SerializeField] private bool _isSectionChangeRequested = false;
    [SerializeField] private BgmLoopInfo _pendingNextBgmLoopInfo;
    [SerializeField] private int _pendingTriggerSample;
    [SerializeField] private int _pendingDestinationSample;

    [Header("Refs")]
    private AudioSource _audioSource;

    private void Awake()
    {
        // インスタンスの用意
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        // コンポーネント取得
        _audioSource = GetComponent<AudioSource>();
        // リストから参照可能な辞書型を生成
        _loopInfoDict = new Dictionary<Phase, BgmLoopInfo>(4);
        foreach (var info in _loopInfoList)
        {
            if (!_loopInfoDict.ContainsKey(info.section))
            {
                _loopInfoDict.Add(info.section, info);
            }
        }
        // INITフェーズ用情報を初期情報としてキャッシュ
        if (_loopInfoDict.TryGetValue(Phase.INIT, out BgmLoopInfo loopInfo))
        {
            _currentBgmLoopInfo = loopInfo;
        }
        else
        {
            Debug.LogError($"INITフェーズのBGM設定が見つかりません。");
        }
    }

    private void Update()
    {
        if (!_audioSource.isPlaying) return;

        int currentSample = _audioSource.timeSamples;
        Debug.Log(currentSample);

        if (_isSectionChangeRequested && currentSample >= _pendingTriggerSample)
        {
            // セクション変更のリクエストがあった場合
            _audioSource.timeSamples = _pendingDestinationSample;

            // キャッシュを新しいセクション情報に更新
            _currentBgmLoopInfo = _pendingNextBgmLoopInfo;

            // 予約フラグをリセット
            _isSectionChangeRequested = false;
        }
        else
        {
            // 通常のBGMループ処理
            if (_audioSource.timeSamples >= _currentBgmLoopInfo.loopEndSample)
            {
                _audioSource.timeSamples = _currentBgmLoopInfo.loopStartSample;
            }
        }
    }
    
    public void PlayBgm()
    {
        Debug.Log("PLAY BGM");
        _audioSource.Play();
    }

    public void StopBgm()
    {
        Debug.Log("STOP BGM");
        _audioSource.Stop();
    }

    /// <summary>
    /// Bgmセクションの変更をリクエストする
    /// </summary>
    public void RequestSectionChangeBgm(Phase nextSection)
    {
        if (!_loopInfoDict.TryGetValue(nextSection, out var nextInfo))
        {
            Debug.LogWarning($"指定されたセクション {nextSection} の設定が見つかりません。");
            return;
        }

        // すでに同じセクションへの変更要求、または初期化前なら無視
        if (_currentBgmLoopInfo.section == nextSection && _audioSource.isPlaying) return;

        // // 初回再生時（まだ音が鳴っていない場合）は即座に開始
        // if (!_audioSource.isPlaying)
        // {
        //     _currentBgmLoopInfo = nextInfo;
        //     audioSource.Play();
        //     audioSource.timeSamples = nextInfo.loopStartSample;
        //     return;
        // }

        int currentSample = _audioSource.timeSamples;

        // === 条件判定：現在のサンプル数に応じて「どのタイミングで、どこに飛ぶか」を確定させる ===
        if (currentSample < _currentBgmLoopInfo.loopHalfSample)
        {
            // 条件A: 現在位置が変更前の loopHalfSample より小さい場合
            // → 変更前の loopHalfSample に達した段階で、変更後セクションの loopHalfSample にジャンプ
            _pendingTriggerSample = _currentBgmLoopInfo.loopHalfSample;
            _pendingDestinationSample = nextInfo.loopHalfSample;
        }
        else
        {
            // 条件B: 現在位置が変更前の loopHalfSample 以上かつ loopEndSample より小さい場合
            // → 変更前の loopEndSample に達した段階で、変更後セクションの loopStartSample にジャンプ
            _pendingTriggerSample = _currentBgmLoopInfo.loopEndSample;
            _pendingDestinationSample = nextInfo.loopStartSample;
        }

        // 変更先情報とリクエストフラグをセット
        _pendingNextBgmLoopInfo = nextInfo;
        _isSectionChangeRequested = true;
    }
}
