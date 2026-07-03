using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, IInitializable
{
    public static GameManager Instance { get; private set; }

    [Header("進行管理")]
    public Phase currentPhase = Phase.INIT;
    public enum Phase
    {
        INIT,
        PREPARATION,
        COMMAND,
        ACTION,
        GAMEOVER,
    };
    public int Turn = 0;
    [Tooltip("経過時間タイマー")]
    public Timer _elapsedTimer = new Timer();

    [Header("操作管理")]
    [Tooltip("操作可否")]
    public bool IsInputLocked = true;
    [Tooltip("ローディング状態")]
    public bool IsLoading;
    [Tooltip("ゲームオーバー状態")]
    public bool IsGameOver;
    
    // [Tooltip("メインビュー操作可否")]
    // public bool isMainViewEnabled = true;

    // 準備フェーズ共通メッセージ
    private string initMessage => "Please place the remaining " + (_mapManager.maxHqCount - _mapManager.PlayerHqCount) + " headquarters units.";

    [Header("Refs")]
    private MapManager _mapManager;
    private TimelineManager _timelineManager;
    private UIManager _uiManager;
    private DialogController _dialogController;
    private InfomationController _infomationController;
    private PlayerManager _playerManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D) && currentPhase == Phase.PREPARATION)
        {
            EnterActionPhase();
        }
        
        if (_elapsedTimer.IsRunning)
        {
            _elapsedTimer.UpdateTick(Time.deltaTime);
            _uiManager.UpdateElapsedTime(_elapsedTimer.RemainingTimeStr);
        }
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _timelineManager = TimelineManager.Instance;
        _uiManager = UIManager.Instance;
        _dialogController = DialogController.Instance;
        _infomationController = InfomationController.Instance;
        _playerManager = PlayerManager.Instance;
    }

    public async UniTask Initialize()
    {
        // 依存関係処理
        ResolveDependencies();
        EnterInitPhase();

        await UniTask.CompletedTask;
    }

    private void ValidateAndShowDialog(int playerHqCount)
    {
        if (playerHqCount < _mapManager.maxHqCount)
        {
            bool isActiveInfo = _infomationController.gameObject.activeSelf;
            if (isActiveInfo)
            {
                _infomationController.UpdateMessage(initMessage);
            }
            else
            {
                _infomationController.Open(initMessage);
            }
        }

        if (playerHqCount == _mapManager.maxHqCount)
        {
            _uiManager.Overlay.Show();
            _infomationController.Close();
            _dialogController.Open(
                isConfirm: true,
                title: "Confirm",
                message: "Setup OK?",
                onConfirm: () =>
                {
                    // アクションイベントの後片付け
                    _mapManager.OnHqCountChanged -= ValidateAndShowDialog;
                    _uiManager.Timeline.Show();
                    _uiManager.Overlay.Hide();
                    EnterPreparationPhase();
                },
                onCancel: () =>
                {
                    UnitSpawnManager.Instance.DespawnAllUnit();
                    _infomationController.Open(initMessage);
                    _uiManager.Overlay.Hide();
                }
            ); 
        }
    }

    private async void EnterInitPhase()
    {
        // 操作制限有効化
        IsInputLocked = true;
        // INITフェーズ時に条件満たした際に実行する処理の登録
        _mapManager.OnHqCountChanged += ValidateAndShowDialog;
        // アナウンスパネル表示
        await _uiManager.BannerView.PlayAnnouncement("INIT");
        // インフォメーションの表示
        _infomationController.Open(initMessage);
        // メニューの初期化
        _uiManager.SwitchMenu(currentPhase);
        // サイドバーの表示
        _uiManager.Sidebar.Show();
        // 準備時間終了後の処理を登録
        _elapsedTimer.OnTimerComplete += EnterActionPhase;
        // 操作制限解除
        IsInputLocked = false;
    }

    private async void EnterPreparationPhase()
    {
        // 操作制限有効化
        IsInputLocked = true;
        // ターン数の加算とUI更新
        Turn++;
        _uiManager.UpdateTurn(Turn);
        // ステータスのリジェネ開始
        _playerManager.StartRegen();
        // フェーズステータス更新
        SwitchPhase(Phase.PREPARATION);
        // アナウンスパネル表示
        await _uiManager.BannerView.PlayAnnouncement("PREPARATION");
        // タイマー開始
        _elapsedTimer.Start(180.0f);
        // 操作制限解除
        IsInputLocked = false;
    }

    private async void EnterActionPhase()
    {
        // 操作制限有効化
        IsInputLocked = true;
        // リジェネ停止
        _playerManager.StopRegen();
        // タイマーリセット
        _elapsedTimer.Reset();
        // フェーズステータス更新
        SwitchPhase(Phase.ACTION);
        // アナウンスパネル表示
        await _uiManager.BannerView.PlayAnnouncement("ACTION");

        if (_timelineManager.TimelineCount > 0)
        {
            await _timelineManager.ProcessTimeline();
            if (IsGameOver) {
                EnterGameOver();
                return;
            };
            _infomationController.Open("All attacks processed.");
        }
        else
        {
            _infomationController.Open("No pending attacks.");
        }

        await UniTask.Delay(2000);
        _infomationController.Close();
        CameraMovement.Instance.MoveTo(TileManager.Instance.PlayerMapLastViewedPosition);
        EnterPreparationPhase();
    }

    public async void EnterGameOver()
    {
        // 操作制限有効化（念のため）
        IsInputLocked = true;
        // フェーズステータス更新
        SwitchPhase(Phase.GAMEOVER);
        // UIの非表示
        _uiManager.Sidebar.Hide();
        _uiManager.SidebarWrapper.Hide();
        _uiManager.Timeline.Hide();
        // ゲームオーバー表示
        await _uiManager.BannerView.PlayAnnouncement("GAME OVER");
        // サイドバーの背景のみ表示
        _uiManager.Sidebar.Show();
        // ダイアログ表示
        _dialogController.Open(
            isConfirm: true,
            title: "Thank you for playing!",
            message: "Retry?",
            onConfirm: () =>
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(currentSceneName);
            },
            onCancel: () =>
            {
                Debug.Log("CANCEL");
            }
        );

    }

    public void SwitchPhase(Phase phase)
    {
        currentPhase = phase;
        _uiManager.SwitchMenu(phase);
        _uiManager.UpdatePhase(phase.ToString());
    }
}
