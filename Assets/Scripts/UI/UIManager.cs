using TMPro;
using UnityEngine;
using Phase = GameManager.Phase;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas")]
    public BannerView BannerView;

    [Header("MainView")]
    public VisibilityController Timeline;
    public VisibilityController Overlay;

    // インスペクターから各パネルを登録
    [Header("Sidebar")]
    public VisibilityController Sidebar;
    public VisibilityController SidebarWrapper;
    public GameObject Turn;
    public GameObject Phase;
    public GameObject ElapsedTime;
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
        SidebarWrapper.Show();
    }

    public void UpdateTurn(int turn)
    {
        TextMeshProUGUI turnText = Turn.GetComponentInChildren<TextMeshProUGUI>();
        turnText.text = $"Turn {turn}";
    }

    public void UpdatePhase(string phase)
    {
        TextMeshProUGUI phaseText = Phase.GetComponentInChildren<TextMeshProUGUI>();
        phaseText.text = phase;
    }

    public void UpdateElapsedTime(string text)
    {
        TextMeshProUGUI elapsedTimeText = ElapsedTime.GetComponentInChildren<TextMeshProUGUI>();
        elapsedTimeText.text = text;
    }

    public void SwitchMenu(Phase phase)
    {
        _initMenu.SetVisible(phase == GameManager.Phase.INIT);
        _preparationMenu.SetVisible(phase == GameManager.Phase.PREPARATION);
        _commandMenu.SetVisible(phase == GameManager.Phase.COMMAND);
    }
}
