using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(VisibilityController))]
public class UnitDetailController : VisibilityController
{
    public static UnitDetailController Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private CanvasGroup _isCallingBadge;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private CanvasGroup _isPendingBadge;
    [SerializeField] private Image _faceIcon;

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

    public void Open(UnitStatsBase stats)
    {
        UnitProfile profile = stats.profile;
        _nameText.text = profile.unitName;
        _isCallingBadge.alpha = profile.unitType == UnitType.Calling ? 1 : 0.3f;
        _hpSlider.maxValue = profile.maxHp;
        _hpSlider.value = stats.hp;
        _hpText.text = $"{stats.hp} / {profile.maxHp}";
        _faceIcon.sprite = profile.FaceIcon;

        if (stats is AttackerStatsBase attackerStats)
        {    
            _isPendingBadge.alpha = attackerStats.IsAttackScheduled ? 1 : 0.3f;
        }
        else
        {
            _isPendingBadge.alpha = 0.3f;
        }

        _visibility.Show();
    }

    public void Close()
    {
        _visibility.Hide();
    }
}
