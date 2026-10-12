using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;

public class InfomationController : MonoBehaviour
{
    public static InfomationController Instance { get; private set; }
    public TextMeshProUGUI messageText;

    private VisibilityController _visibility;
    private CancellationTokenSource _cts;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _visibility = GetComponent<VisibilityController>();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
    }

    public void Open(string message)
    {
        // 既存のタイムアウトがあればキャンセル
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        messageText.text = message;
        _visibility.Show();
    }

    public async UniTask OpenAsync(string message, float duration = 2.0f)
    {
        Open(message);

        bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: _cts.Token).SuppressCancellationThrow();
        if (!canceled) Close();
    }

    public void Close()
    {
        _cts?.Cancel();
        _visibility.Hide();
    }

    public void UpdateMessage(string message)
    {
        messageText.text = message;
    }
}
