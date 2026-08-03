using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

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
    [Tooltip("フェーズタイマー")]
    private Timer _phaseTimer = new Timer();
    [Tooltip("ターンの制限時間")]
    private float _timeLimit = 60.0f;

    [Header("操作管理")]
    [Tooltip("操作可否")]
    public bool IsInputLocked = true;
    [Tooltip("ローディング状態")]
    public bool IsLoading;
    [Tooltip("ゲームオーバー状態")]
    public bool IsGameOver;

    // 準備フェーズ共通メッセージ
    private string initMessage => "Please place the remaining " + (_mapManager.maxHqCount - _mapManager.PlayerHqCount) + " headquarters units.";

    [Header("Refs")]
    private MapManager _mapManager;
    private TimelineManager _timelineManager;
    private UIManager _uiManager;
    private DialogController _dialogController;
    private InfomationController _infomationController;
    private PlayerManager _playerManager;

    [SerializeField]
    private List<Tile> CallingTiles;

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
        
        if (_phaseTimer.IsRunning)
        {
            _phaseTimer.UpdateTick(Time.deltaTime);
            _uiManager.UpdateElapsedTime(_phaseTimer.RemainingTimeStr);
        }
    }

    private void OnDestroy()
    {
        //  経過時間取得処理のイベント解除
        if (_timelineManager != null) {
            _timelineManager.OnRequestPhaseElapsedTime -= GetPhaseElapsedTime;
            _timelineManager.OnGameOverConditionMet -= NotifyGameOver;
        }
    }

    public async UniTask Initialize()
    {
        // 依存関係処理
        ResolveDependencies();
        // 経過時間取得処理をタイムラインマネージャーにイベントとして登録
        if (_timelineManager != null) {
            _timelineManager.OnRequestPhaseElapsedTime += GetPhaseElapsedTime;
            _timelineManager.OnGameOverConditionMet += NotifyGameOver;
        }
        // 初期化フェーズへ移行
        EnterInitPhase();

        await UniTask.CompletedTask;
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

    private void NotifyGameOver()
    {
        IsGameOver = true;
    }

    private float GetPhaseElapsedTime()
    {
        return _phaseTimer.ElapsedTime;
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

    private void SetCallUnitTimersPaused(bool isPaused)
    {
        // List<Tile> CallingTiles = _mapManager.GetCallingTiles(Owner.Player);
        // CallingTiles.AddRange(_mapManager.GetCallingTiles(Owner.Enemy));

        List<Tile> CallingTiles = _mapManager.GetCallingTiles(Owner.Player);
        CallingTiles.AddRange(_mapManager.GetCallingTiles(Owner.Enemy));

        foreach (Tile tile in CallingTiles)
        {
            if (tile.Unit is not CallUnit callUnit)
            {
                throw new InvalidOperationException("タイマー処理を実行できません：登録されているユニットはCallではありません。");
            }
            if (isPaused) callUnit.Controller.PauseTimer();
            else callUnit.Controller.ResumeTimer();
        }
    }

    private async UniTask EnterPhase(Phase phase, Func<UniTask> openingEvents, Func<UniTask> closedEvents)
    {
        // 操作制限有効化
        IsInputLocked = true;

        await UniTask.Delay(TimeSpan.FromSeconds(0.7f));

        // アナウンスパネルオープン処理
        _uiManager.BannerView.PlayOpenAnimationAsync(phase.ToString()).Forget();
        // パネルなイベント処理の実行
        if (openingEvents == null) throw new Exception("フェーズ処理が未登録です。");
        // イベント処理の実行完了と2秒のディレイを待つ
        UniTask eventTask = openingEvents.Invoke();
        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(2.0f));
        await UniTask.WhenAll(eventTask, delayTask);

        // アナウンスパネルクローズ処理
        await _uiManager.BannerView.PlayCloseAnimationAsync();
        // パネルなイベント処理の実行
        if (closedEvents == null) throw new Exception("フェーズ処理が未登録です。");
        await closedEvents.Invoke();

        // 操作制限解除
        IsInputLocked = false;
    }

    private async void EnterInitPhase()
    {
        Func<UniTask> openingEvents = () =>
        {
            // INITフェーズ時に条件満たした際に実行する処理の登録
            _mapManager.OnHqCountChanged += ValidateAndShowDialog;
            return UniTask.CompletedTask;
        };
        Func<UniTask> closedEvents = () =>
        {
            // インフォメーションの表示
            _infomationController.Open(initMessage);
            // メニューの初期化
            _uiManager.SwitchMenu(Phase.INIT);
            // サイドバーの表示
            _uiManager.Sidebar.Show();
            // 準備時間終了後の処理を登録
            _phaseTimer.OnTimerComplete += EnterActionPhase;
            return UniTask.CompletedTask;
        };
        await EnterPhase(Phase.INIT, openingEvents, closedEvents);
    }

    private async void EnterPreparationPhase()
    {
        Func<UniTask> openingEvents = () =>
        {
            // ターン数の加算とUI更新
            Turn++;
            _uiManager.UpdateTurn(Turn);
            // ステータスのリジェネ開始
            _playerManager.StartRegen();
            return UniTask.CompletedTask;
        };
        Func<UniTask> closedEvents = () =>
        {
            // フェーズステータス更新
            SwitchPhase(Phase.PREPARATION);
            // タイマー開始
            _phaseTimer.Start(_timeLimit);
            // 呼出中ユニットのタイマーを再開
            SetCallUnitTimersPaused(false);
            return UniTask.CompletedTask;
        };
        await EnterPhase(Phase.PREPARATION, openingEvents, closedEvents);
    }

    private async void EnterActionPhase()
    {
        Func<UniTask> openingEvents = () =>
        {
            // 呼出中ユニットのタイマーを一時停止
            SetCallUnitTimersPaused(true);
            // リジェネ停止
            _playerManager.StopRegen();
            // タイマーリセット
            _phaseTimer.Reset();
            // タイムラインの集計
            _timelineManager.SortAndCombineTimelines();
            return UniTask.CompletedTask;
        };
        Func<UniTask> closedEvents = async () =>
        {
            // フェーズステータス更新
            SwitchPhase(Phase.ACTION);
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
        };
        await EnterPhase(Phase.ACTION, openingEvents, closedEvents);
    }

    private async void EnterGameOver()
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
