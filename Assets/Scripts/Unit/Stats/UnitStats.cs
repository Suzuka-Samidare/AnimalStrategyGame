using UnityEngine;
using Guid = System.Guid;

public class UnitStats : MonoBehaviour
{
    [Header("静的ステータス")]
    [Tooltip("UID")] private string _uuid;
    [Tooltip("基本ステータス")] public UnitProfile profile;

    // [Header("タイプ別静的ステータス")]
    // [Tooltip("攻撃ステータス"), SerializeField] private AttackProfile attackProfile;
    // [Tooltip("防衛ステータス"), SerializeField] private DefenceProfile defenceProfile;
    // [Tooltip("サポートステータス"), SerializeField] private SupportProfile supportProfile;

    [Header("動的ステータス")]
    [Tooltip("耐久値")] public float hp;

    [Header("Refs")]
    private AttackUnitStats _attackUnitStats;
    private DefenceUnitStats _defenceUnitStats;
    private SupportUnitStats _supportUnitStats;

    private void Awake()
    {
        _uuid = Guid.NewGuid().ToString();
    }

    public void Initialize(UnitData unitData)
    {
        // 基本ステータスの初期化
        this.profile = unitData.profile;
        this.hp = profile.maxHp;

        // タイプ別ステータスの初期化
        if (unitData is AttackUnitData attackUnitData)
        {
            _attackUnitStats = GetComponent<AttackUnitStats>();
            _attackUnitStats.profile = attackUnitData.attackProfile;
        }
        if (unitData is DefenceUnitData defenceUnitData)
        {
            _defenceUnitStats = GetComponent<DefenceUnitStats>();
            _defenceUnitStats.profile = defenceUnitData.defenceProfile;
        }
        if (unitData is SupportUnitData supportUnitData)
        {
            _supportUnitStats = GetComponent<SupportUnitStats>();
            _supportUnitStats.profile = supportUnitData.supportProfile;
        }
    }

    public void Initialize(UnitProfile profile)
    {
        this.profile = profile;
        hp = profile.maxHp;
    }

    // TODO: タイプ別静的ステータスへのアクセス方法を考える
}
