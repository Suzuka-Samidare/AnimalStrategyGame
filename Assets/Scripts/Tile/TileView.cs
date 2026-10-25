using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TileView : MonoBehaviour
{
    [Header("タイル色設定")]
    [Tooltip("ストライプ色の自動設定")]
    public bool isAutoStripedColor;
    [Tooltip("タイルベース色")]
    public Color mainColor;
    [Tooltip("明滅色（自マップ用）")]
    public Color blinkAllyColor;
    [Tooltip("明滅色（敵マップ用）")]
    public Color blinkEnemyColor;
    // [Tooltip("不可視状態時の色")]
    // public Color invisibleColor;

    [Tooltip("ストライプ色"), SerializeField]
    private Color stripedColor;
    [Tooltip("最終タイル色"), SerializeField]
    private Color baseColor;

    [Header("状態管理")]
    [SerializeField, Tooltip("現在のベースカラー")]
    private Color currentBaseColor;
    [SerializeField, Tooltip("現在の上面カラー")]
    private Color currentTopColor;

    [Header("Refs")]
    private Renderer objectRenderer;
    private MaterialPropertyBlock propBlock;
    // シェーダーのプロパティ名（Shader GraphのReferenceで設定したもの）
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
    private GameManager _gameManager;
    private TileStats _tileStats;

    [Header("Token")]
    private CancellationTokenSource cts;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        _tileStats = GetComponent<TileStats>();
    }

    private void Start()
    {
        _gameManager = GameManager.Instance;
        if (_gameManager == null) throw new Exception("GameManagerがありません。");

        InitializeTileColor();
    }

    private void OnValidate()
    {
        if (!isAutoStripedColor) return;
        // ストライプ色の計算
        stripedColor = mainColor * 0.9f;
        stripedColor.a = 1f;
    }

    private void OnDestroy()
    {
        CancelAndDisposeCts();
    }

    /// <summary>
    /// 選択中のマスを解除する
    /// </summary>
    private void InitializeTileColor()
    {
        if ((_tileStats.GridPos.x + _tileStats.GridPos.y) % 2 == 0)
        {
            baseColor = mainColor;
        }
        else
        {
            baseColor = stripedColor;
        }

        // 初期状態のセット
        currentBaseColor = baseColor;
        currentTopColor = baseColor;
        ApplyColors();
    }

    /// <summary>
    /// セレクト状態の点滅（プレイヤー側のタイルでのみ使用）
    /// </summary>
    public void StartSelectedBlink()
    {
        Blink(blinkAllyColor).Forget();
    }

    /// <summary>
    /// ターゲット状態の点滅（敵側のタイルでのみ使用）
    /// </summary>
    public void StartTargetBlink()
    {
        Blink(blinkEnemyColor).Forget();
    }

    /// <summary>
    /// 点滅表示の解除（共通）
    /// </summary>
    public void StopBlink()
    {
        CancelAndDisposeCts();
        ResetColors();
    }

    /// <summary>
    /// 元のタイル色に戻す
    /// </summary>
    private void ResetColors()
    {
        currentBaseColor = baseColor;
        currentTopColor = baseColor;
        ApplyColors();
    }

    /// <summary>
    /// 点滅表示処理
    /// </summary>
    private async UniTaskVoid Blink(Color blinkColor)
    {
        // キャンセルトークンの再準備
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();
        var token = cts.Token;

        try
        {
            while (true)
            {
                float time = Mathf.PingPong(Time.time, 1.0f);
                currentTopColor = Color.Lerp(baseColor, blinkColor, time);
                ApplyColors();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }
        }
        catch (OperationCanceledException) {}
    }

    /// <summary>
    /// マテリアル色の更新
    /// </summary>
    private void ApplyColors()
    {
        // objectRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(BaseColorId, currentBaseColor);
        propBlock.SetColor(TopColorId, currentTopColor);
        objectRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// キャンセルトークン処分処理
    /// </summary>
    private void CancelAndDisposeCts()
    {
        if (cts == null) return;
        cts.Cancel();
    }
}
