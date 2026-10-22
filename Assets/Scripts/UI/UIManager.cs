using TMPro;
using UnityEngine;
using Phase = GameManager.Phase;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas")]
    public BannerView BannerView;

    [Header("MainView")]
    public VisibilityController MainView;
    public VisibilityController Timeline;
    public VisibilityController Overlay;
    public VisibilityController BottomLayoutGroup;

    // インスペクターから各パネルを登録
    [Header("Text")]
    public TextMeshProUGUI TurnText;
    public TextMeshProUGUI PhaseText;
    public TextMeshProUGUI RemainingTimeText;

    [Header("BottomMenu")]
    [SerializeField] private VisibilityController _menuBg;
    [SerializeField] private VisibilityController _initMenu;
    [SerializeField] private VisibilityController _preparationMenu;
    [SerializeField] private VisibilityController _commandMenu;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Timeline.Show();
        BottomLayoutGroup.Show();
    }

    public void UpdateTurn(int turn)
    {
        TurnText.text = $"Turn {turn}";
    }

    public void UpdatePhase(string phase)
    {
        PhaseText.text = phase;
    }

    public void UpdateRemainingTime(string text)
    {
        RemainingTimeText.text = text;
    }

    public void SwitchMenu(Phase phase)
    {
        _menuBg.SetVisible(phase == Phase.INIT || phase == Phase.PREPARATION || phase == Phase.COMMAND);
        _initMenu.SetVisible(phase == Phase.INIT);
        _preparationMenu.SetVisible(phase == Phase.PREPARATION);
        _commandMenu.SetVisible(phase == Phase.COMMAND);
    }
}
