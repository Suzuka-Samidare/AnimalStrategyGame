using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FloatingTextView : MonoBehaviour
{
    [Header("アニメーション設定")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.1f;
    [SerializeField] private float _viewDuration = 1.5f;
    [SerializeField] private float _moveSpeed = 50f;
    private Vector3 _animationOffset;
    [SerializeField] private float _startFontSize = 150f;
    [SerializeField] private float _endFontSize = 80f;

    [Header("カラー")]
    [SerializeField] private Color _faceColor;
    [SerializeField] private Color _outlineColor;

    [Header("状態管理")]
    private Coroutine _fadeRoutine;
    private bool _isRoutineFinished;

    [Header("Ref")]
    [SerializeField] private Vector3 _position;
    [SerializeField] private TextMeshProUGUI _textMesh;
    [SerializeField] private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _textMesh = GetComponentInChildren<TextMeshProUGUI>();

        if (!_canvasGroup) throw new Exception("CanvasGroupの取得失敗");
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (!_textMesh) throw new Exception("TextMeshProUGUIの取得失敗");
        _outlineColor = _textMesh.outlineColor;
    }

    void Update()
    {
        if (_position != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_position);
            Vector3 fixOffset = new Vector3(0f, 20f, 0f); // 20ピクセル分
            transform.position = screenPos + fixOffset + _animationOffset;
        }
    }

    public async UniTask SetupAsync(Vector3 position, float amount, Color color)
    {
        _isRoutineFinished = false;
        _position = position;
        _faceColor = color;

        if (_textMesh == null) throw new Exception("TextMeshProUGUIの取得失敗");
        _textMesh.faceColor = Color.white;
        _textMesh.outlineColor = Color.white;
        _textMesh.text = amount.ToString();

        if (_position == null) throw new Exception("タイル座標が取得できませんでした");
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_position);
        transform.position = screenPos;

        _fadeRoutine = StartCoroutine(FullFadeRoutine());

        await UniTask.WaitUntil(() => _isRoutineFinished == true);

        Debug.Log("演出が完全に終わったよ！✨");
    }

    private IEnumerator FullFadeRoutine()
    {
        // 1.FadeIn ===============================================
        float elapsedTime = 0;
        while (elapsedTime < _fadeInDuration)
        {
            // 対象がDestroyなどで消えていた場合の後片付け
            if (_canvasGroup == null)
            {
                _fadeRoutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float time = Mathf.Clamp01(elapsedTime / _fadeInDuration);
            
            _canvasGroup.alpha = time; // alphaを少しずつ加算
            _textMesh.fontSize = Mathf.Lerp(_startFontSize, _endFontSize, time);
            _textMesh.faceColor = Color.Lerp(Color.white, _faceColor, time);
            _textMesh.outlineColor = Color.Lerp(Color.white, _outlineColor, time);
            _animationOffset += new Vector3(0f, 0.5f, 0f) * (_moveSpeed * Time.deltaTime);

            yield return null; // 次のフレームまで待機
        }
        _canvasGroup.alpha = 1f; // 最後に確実に1にする
        _textMesh.fontSize = _endFontSize;
        
        // 2. Wait ==================================================
        elapsedTime = 0;
        while (elapsedTime < _viewDuration)
        {
            elapsedTime += Time.deltaTime;
            _animationOffset += new Vector3(0f, 0.5f, 0f) * (_moveSpeed * Time.deltaTime);

            yield return null;
        }

        // 3. FadeOut ===============================================
        elapsedTime = 0;
        while (elapsedTime < _fadeOutDuration)
        {
            if (_canvasGroup == null)
            {
                _fadeRoutine = null;
                yield break;
            }
            elapsedTime += Time.deltaTime;
            float time = Mathf.Clamp01(elapsedTime / _fadeOutDuration);
            
            _canvasGroup.alpha = 1f - time;
            _animationOffset += new Vector3(0f, 0.5f, 0f) * (_moveSpeed * Time.deltaTime);

            yield return null;
        }
        _canvasGroup.alpha = 0f;

        // 4. After Care =============================================
        _isRoutineFinished = true;
        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
        Destroy(gameObject);
    }

    // public void UpdateDamagePosition(Transform unitTransform, RectTransform uiText)
    // {
    //     // ユニットの少し上（頭上）の座標を取得
    //     Vector3 worldPos = unitTransform.position + Vector3.up * 2.0f;

    //     // スクリーン座標に変換
    //     Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

    //     // カメラの裏側にいる時は表示しない（これ大事！）
    //     if (screenPos.z < 0) {
    //         uiText.gameObject.SetActive(false);
    //         return;
    //     }

    //     // UIの座標にセット
    //     uiText.position = screenPos;
    // }
}
