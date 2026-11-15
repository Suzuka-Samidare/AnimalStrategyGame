using System;
using System.Collections.Generic;
using UnityEngine;
using Phase = GameManager.Phase;

public enum SESoundID
{
    TileSelect,
    ButtonApprove,
    ButtonCancel,
    SquidInkBomb,
}

public enum VoiceID
{
    ColobusChatter,
    TaipanChatter,
    SquisAttack,
    SquidChatter,
    HerringChatter,
}

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

    [Serializable]
    private struct SEInfo
    {
        public SESoundID id;
        public AudioClip clip;
    }

    [Serializable]
    private struct VoiceInfo
    {
        public VoiceID id;
        public AudioClip clip;
    }

    [Header("BGM情報")]
    [SerializeField] private AudioClip _mainBgm;
    [SerializeField] private List<BgmLoopInfo> _loopInfoList = new List<BgmLoopInfo>(4);
    private Dictionary<Phase, BgmLoopInfo> _loopInfoDict;

    [Header("現在のBGMループ情報")]
    [SerializeField] private BgmLoopInfo _currentBgmLoopInfo;

    [Header("次回のBGMループ情報")]
    [SerializeField] private bool _isSectionChangeRequested = false;
    [SerializeField] private BgmLoopInfo _pendingNextBgmLoopInfo;
    [SerializeField] private int _pendingTriggerSample;
    [SerializeField] private int _pendingDestinationSample;

    [Header("SE情報")]
    [SerializeField] private List<SEInfo> _seInfoList = new List<SEInfo>();
    private Dictionary<SESoundID, AudioClip> _seInfoDict;

    [Header("ボイス情報")]
    [SerializeField] private List<VoiceInfo> _voiceInfoList = new List<VoiceInfo>();
    private Dictionary<VoiceID, AudioClip> _voiceInfoDict;

    [Header("Refs")]
    [SerializeField] private AudioSource _bgmAudioSource;
    [SerializeField] private AudioSource _seAudioSource;
    [SerializeField] private AudioSource _voiceAudioSource;

    private void Awake()
    {
        // インスタンスの用意
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        // BGMリストから参照可能なBGM辞書を生成
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
        // SEリストから参照可能なSE辞書を作成
        _seInfoDict = new Dictionary<SESoundID, AudioClip>(_seInfoList.Count);
        foreach (var info in _seInfoList)
        {
            _seInfoDict[info.id] = info.clip;
        }
        // ボイスリストから参照可能なボイス辞書を作成
        _voiceInfoDict = new Dictionary<VoiceID, AudioClip>(_voiceInfoList.Count);
        foreach (var info in _voiceInfoList)
        {
            _voiceInfoDict[info.id] = info.clip;
        }
    }

    private void Update()
    {
        if (!_bgmAudioSource.isPlaying) return;

        int currentSample = _bgmAudioSource.timeSamples;
        // Debug.Log(currentSample);

        if (_isSectionChangeRequested && currentSample >= _pendingTriggerSample)
        {
            // セクション変更のリクエストがあった場合
            _bgmAudioSource.timeSamples = _pendingDestinationSample;

            // キャッシュを新しいセクション情報に更新
            _currentBgmLoopInfo = _pendingNextBgmLoopInfo;

            // 予約フラグをリセット
            _isSectionChangeRequested = false;
        }
        else
        {
            // 通常のBGMループ処理
            if (_bgmAudioSource.timeSamples >= _currentBgmLoopInfo.loopEndSample)
            {
                _bgmAudioSource.timeSamples = _currentBgmLoopInfo.loopStartSample;
            }
        }
    }
    
    public void PlayBGM()
    {
        Debug.Log("PLAY BGM");
        _bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        Debug.Log("STOP BGM");
        _bgmAudioSource.Stop();
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
        if (_currentBgmLoopInfo.section == nextSection && _bgmAudioSource.isPlaying) return;

        int currentSample = _bgmAudioSource.timeSamples;

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

    /// <summary>
    /// SEの再生（鳴らす音源を指定）
    /// </summary>
    public void PlaySE(SESoundID id)
    {
        if (_seInfoDict.TryGetValue(id, out var clip))
        {
            Debug.Log("PLAY SE");
            _seAudioSource.PlayOneShot(clip);
        }
    }

        /// <summary>
    /// ボイスの再生（鳴らす音源を指定）
    /// </summary>
    public void PlayVoice(VoiceID id)
    {
        if (_voiceInfoDict.TryGetValue(id, out var clip))
        {
            Debug.Log("PLAY Voice");
            _seAudioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 動物の待機ボイスの再生
    /// </summary>
    public void PlayVoiceChatter(UnitType unitType)
    {
        if (unitType == UnitType.Colobus) PlayVoice(VoiceID.ColobusChatter);
        if (unitType == UnitType.Taipan) PlayVoice(VoiceID.TaipanChatter);
        if (unitType == UnitType.Squid) PlayVoice(VoiceID.SquidChatter);
    }
}
