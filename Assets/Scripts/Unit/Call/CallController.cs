using System;
using UnityEngine;

public class CallController : UnitControllerBase
{
    [Header("タイマー")]
    private Timer _timer = new Timer();

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
        // ローカル変数で宣言して、ラムダ式内でイベント解除するActionを参照できるようにする
        Action wrapperHandler = null;
        // 本来の処理及びイベント解除を合わせたラッパー
        wrapperHandler = () =>
        {
            onCompleteCallback?.Invoke();
            _timer.OnTimerComplete -= wrapperHandler;
            _timer.Reset();
        };
        // イベントの登録
        _timer.OnTimerComplete += wrapperHandler;
        _timer.Start(callTime);
    }
}
