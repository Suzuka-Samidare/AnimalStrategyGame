using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using PlayerActions = GameInputs.PlayerActions;

public class InputHandler : MonoBehaviour
{
    public static event Action<Vector2> OnSelect;     // 短押し：選択
    public static event Action<Vector2> OnMoveUpdate;   // ドラッグ：カメラ平行移動
    public static event Action<Vector2> OnAngleUpdate; // ドラッグ：カメラ回転
    public static event Action<float> OnZoomUpdate;

    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private Vector2 _startPos;
    [SerializeField] private bool _isDragging;
    [SerializeField] private bool _isOverUI;
    [SerializeField] private bool _isPrimaryPressing; // 左クリ or 1本指
    [SerializeField] private bool _isSecondaryPressing; // 右クリ or 2本指
    [SerializeField] private float _prevPinchDist;

    [SerializeField] private PlayerActions controls;

    void Start()
    {
        controls = InputProvider.Controls.Player;

        // メイン操作（左クリ / 1本指）
        controls.MainInteract.started += StartMainInteract;
        controls.MainInteract.canceled += EndMainInteract;

        // サブ操作（右クリ / 2本指）
        controls.SubInteract.started += StartSubInteract;
        controls.SubInteract.canceled += EndSubInteract;
    }

    private void StartMainInteract(InputAction.CallbackContext ctx)
    {
        // 操作ロック中の場合は処理を行わない
        if (GameManager.Instance.IsInputLocked) return;

        // 現在のポインタ（マウス/タッチ）の画面座標を取得
        Vector2 currentPoint = controls.Point.ReadValue<Vector2>();
        // WebGL/モバイル対応のUI接触チェック
        if (IsPointerOverUIElement(currentPoint))
        {
            _isOverUI = true;
            return;
        }
        _isPrimaryPressing = true;
        _startPos = currentPoint;
    }

    private void EndMainInteract(InputAction.CallbackContext _)
    {
        if (!GameManager.Instance.IsInputLocked && !_isOverUI && !_isDragging)
        {
            OnSelect?.Invoke(controls.Point.ReadValue<Vector2>());
        }
        _isOverUI = false;
        _isDragging = false;
        _isPrimaryPressing = false;
    }

    private void StartSubInteract(InputAction.CallbackContext _)
    {
        // サブ操作開始時も現在の座標でUIチェック
        Vector2 currentPoint = controls.Point.ReadValue<Vector2>();

        if (IsPointerOverUIElement(currentPoint))
        {
            _isOverUI = true;
            return;
        }
        _isSecondaryPressing = true;
    }

    private void EndSubInteract(InputAction.CallbackContext _)
    {
        _isOverUI = false;
        _isSecondaryPressing = false;
        _prevPinchDist = 0;
    }

    void Update()
    {
        // UI上で操作している場合は処理しない
        if (_isOverUI || GameManager.Instance.IsInputLocked) return;

        // --- マウスホイール (PC用) ---
        float scroll = controls.Scroll.ReadValue<Vector2>().y;
        if (scroll != 0)
        {
            OnZoomUpdate?.Invoke(scroll * 0.1f);
        }

        // 2. 2本指操作 (モバイル用：回転 & ピンチズーム)
        if (_isSecondaryPressing) 
        {
            // 回転の通知
            Vector2 delta = controls.Delta.ReadValue<Vector2>();
            if (delta != Vector2.zero)
            {
                OnAngleUpdate?.Invoke(delta);
            }

            // ピンチズーム計算 (モバイルのTouch1がある時だけ実行)
            Vector2 t1 = controls.Touch1Point.ReadValue<Vector2>();
            if (t1 != Vector2.zero) 
            {
                Vector2 t0 = controls.Point.ReadValue<Vector2>();
                float currentDist = Vector2.Distance(t0, t1);
                if (_prevPinchDist > 0) 
                {
                    OnZoomUpdate?.Invoke((currentDist - _prevPinchDist) * 0.05f);
                }
                _prevPinchDist = currentDist;
            }
            return; 
        }

        // --- 1本指/左クリ操作 (移動) ---
        if (_isPrimaryPressing) {
            Vector2 currentPos = controls.Point.ReadValue<Vector2>();
            if (!_isDragging && Vector2.Distance(_startPos, currentPos) > dragThreshold)
            {
                _isDragging = true;
            }
            if (_isDragging)
            {
                OnMoveUpdate?.Invoke(controls.Delta.ReadValue<Vector2>());
            }
        }
    }

    /// <summary>
    /// WebGL・モバイル・PC全環境対応のUI接触チェック
    /// </summary>
    private bool IsPointerOverUIElement(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        // 標準関数も併せてチェック（PCなどで確実に弾くため）
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // 指定座標に直接UIレイを飛ばして多重チェック（WebGL/モバイルのタイムラグ対策）
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        return results.Count > 0;
    }
}