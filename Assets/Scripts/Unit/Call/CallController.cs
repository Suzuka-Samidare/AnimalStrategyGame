using System;
using UnityEngine;

public class CallController : UnitControllerBase
{
    [Header("タイマー")]
    private Timer _timer = new Timer();

    private Action _activeTimerHandler;

    private void Update()
    {
        // タイマーを進める
        _timer.UpdateTick(Time.deltaTime);

        // UI表示などのために現在の時間をログに出す
        // if (_timer.IsRunning)
        // {
        //     Debug.Log($"残り時間: {_timer.CurrentTime}秒");
        // }

        // // スペースキーで一時停止/再開を切り替え
        // if (Input.GetKeyDown(KeyCode.PageUp))
        // {
        //     _timer.IsPaused = !_timer.IsPaused;
        //     Debug.Log(_timer.IsPaused ? "一時停止" : "再開");
        // }
    }

    public void StartTimer(float callTime, Action onCompleteCallback)
    {
        ClearActiveTimer();

        // 本来の処理及びイベント解除を合わせたラッパー
        _activeTimerHandler = () =>
        {
            onCompleteCallback?.Invoke();
            ClearActiveTimer();
        };
        // イベントの登録
        _timer.OnTimerComplete += _activeTimerHandler;
        _timer.Start(callTime);
    }

    /// <summary>
    /// 現在アクティブなタイマーのイベント解除とリセットを行う
    /// </summary>
    public void ClearActiveTimer()
    {
        if (_activeTimerHandler != null)
        {
            _timer.OnTimerComplete -= _activeTimerHandler;
            _activeTimerHandler = null;
        }
        _timer.Reset(); // Timerクラス側に Stop() や時間を0にする処理があると想定
    }
}
