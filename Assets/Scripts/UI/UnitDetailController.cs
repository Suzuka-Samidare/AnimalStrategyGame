using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(VisibilityController))]
public class UnitDetailController : VisibilityController
{
    public static UnitDetailController Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private VisibilityController _isCallingBadge;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TextMeshProUGUI _hpText;

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
        _isCallingBadge.SetVisible(profile.unitType == UnitType.Calling);
        _hpSlider.maxValue = profile.maxHp;
        _hpSlider.value = stats.hp;
        _hpText.text = $"{stats.hp} / {profile.maxHp}";

        _visibility.Show();
    }

    public void Close()
    {
        _visibility.Hide();
    }
}
