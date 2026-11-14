using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(VisibilityController))]
public class DialogController : MonoBehaviour
{
    public static DialogController Instance { get; private set; }

    public bool IsOpen => _visibility.IsVisible;

    // UIコンポーネントへの参照（Inspectorで設定）
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText; // メッセージテキスト
    [SerializeField] private Button _okButton; // OKボタン
    [SerializeField] private Button _cancelButton;  // キャンセルボタン

    // 外部から処理を受け取るためのデリゲート
    private Action _onConfirmAction;
    private Action _onCancelAction;

    private VisibilityController _visibility;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _visibility = GetComponent<VisibilityController>();
    }

    // 外部からデータを渡してダイアログを開くためのメインメソッド
    public void Open(bool isConfirm, string title, string message, Action onConfirm, Action onCancel = null)
    {
        // 1. データ（表示内容）の設定
        _titleText.text = title;
        _messageText.text = message;

        // 2. コールバック（実行される処理）の設定
        _onConfirmAction = onConfirm;
        // AlertタイプでonCancelがnullの場合に備えて、null合体演算子で処理
        _onCancelAction = onCancel ?? (() => {}); // nullの場合は空の処理を設定

        // Alertの場合、キャンセルボタンを非表示にする
        _cancelButton.gameObject.SetActive(isConfirm);
        
        // Confirmタイプの場合、OKボタンとCancelボタンの間隔を調整する処理を追加できます
        // 例: レイアウトコンポーネントの調整など...
        
        // 4. UIリスナーの登録（共通処理）
        _okButton.onClick.RemoveListener(OnConfirmButtonClicked);
        _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        
        _okButton.onClick.AddListener(OnConfirmButtonClicked);
        _cancelButton.onClick.AddListener(OnCancelButtonClicked);

        // ダイアログの表示
        _visibility.Show();
    }

    public void Close()
    {
        _visibility.Hide();
    }
    
    // ボタンクリック時の処理（変更なし）
    private void OnConfirmButtonClicked()
    {
        _onConfirmAction?.Invoke(); 
        Close();
    }
    
    private void OnCancelButtonClicked()
    {
        _onCancelAction?.Invoke(); 
        Close();
    }
}
