using System;
using System.ComponentModel;
using UnityEngine;
using Phase = GameManager.Phase;

public class TileView : MonoBehaviour
{
    [Header("タイル色設定")]
    [Tooltip("タイルベース色")]
    public Color mainColor;
    [Tooltip("明滅色（自マップ用）")]
    public Color blinkAllyColor;
    [Tooltip("明滅色（敵マップ用）")]
    public Color blinkEnemyColor;
    [Tooltip("不可視状態時の色")]
    public Color invisibleColor;

    [Tooltip("ストライプ色"), SerializeField]
    private Color stripedColor;
    [Tooltip("最終タイル色")]
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
    private TileController _tileController;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        _tileController = GetComponent<TileController>();

        if (_tileController == null) throw new Exception("TileControllerがありません。");
    }

    private void Start()
    {
        _gameManager = GameManager.Instance;
        if (_gameManager == null) throw new Exception("GameManagerがありません。");

        InitializeTileColor();
    }

    private void Update()
    {
        if (_tileController.isSelected && _gameManager.currentPhase == Phase.INIT)
        {
            Blink(blinkAllyColor);
            return;
        }

        if (_tileController.isSelected && _gameManager.currentPhase == Phase.PREPARATION)
        {
            Blink(blinkAllyColor);
            return;
        }

        if (_tileController.isTargeted && _gameManager.currentPhase == Phase.COMMAND)
        {
            Blink(blinkEnemyColor);
            return;
        }
    }

    private void OnValidate()
    {
        // ストライプ色の計算
        stripedColor = mainColor * 0.9f;
        stripedColor.a = 1f;
    }

    private void InitializeTileColor()
    {
        if ((_tileController.gridPos.x + _tileController.gridPos.y) % 2 == 0)
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

    public void RefreshVisual()
    {
        if (_tileController.isSelected) return;

        if (_tileController.owner == TileController.TileOwner.Enemy && !_tileController.isRevealed)
        {
            currentBaseColor = invisibleColor;
            currentTopColor = invisibleColor;
        }
        else
        {
            currentBaseColor = baseColor;
            currentTopColor = baseColor;
        }     
        ApplyColors();
    }

    private void Blink(Color blinkColor)
    {
        float time = Mathf.PingPong(Time.time, 1.0f);
        Color normalColor;
        if (_tileController.owner == TileController.TileOwner.Enemy && !_tileController.isRevealed)
        {
            normalColor = invisibleColor;
        }
        else
        {
            normalColor = baseColor;
        }  

        currentTopColor = Color.Lerp(normalColor, blinkColor, time);

        ApplyColors();
    }

    private void ApplyColors()
    {
        // objectRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(BaseColorId, currentBaseColor);
        propBlock.SetColor(TopColorId, currentTopColor);
        objectRenderer.SetPropertyBlock(propBlock);
    }
}
