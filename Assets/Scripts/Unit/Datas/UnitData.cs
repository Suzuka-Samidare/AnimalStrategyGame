using System;
using UnityEngine;
using MapId = MapManager.MapId;

public enum UnitType
{
    Calling = 0,
    Headquarter = 1,
    Colobus = 2,
    Gecko = 3,
    Herring = 4,
    Muskrat = 5,
    Pudu = 6,
    Sparrow = 7,
    Squid = 8,
    Taipan = 9,
}

public enum AttackType
{
    Single,     // 単体
    Square,     // 正方形
    Manhattan,  // 菱形
    Cross       // 十字
}

[Serializable]
public struct AttackRange
{
    public int min;
    public int max;
}

[Serializable]
public struct UnitProfile
{
    [Tooltip("マップID")] public MapId id;
    [Tooltip("ユニットID")] public UnitType unitType;
    [Tooltip("ユニット名")] public string unitName;
    [Tooltip("最大耐久値")] public float maxHp;

    // TODO: 後で消す
    [Tooltip("攻撃力")] public float power;
    [Tooltip("攻撃消費エネルギー")] public int energy;
    [Tooltip("タイムラインへの影響速度")] public float atkDelay;
    [Tooltip("攻撃の種類")] public AttackType atkType;
    [Tooltip("範囲攻撃の距離")] public AttackRange atkRange;
}


// 右クリックメニューからアセットを作成するための属性
[CreateAssetMenu(fileName = "UnitData", menuName = "ScriptableObjects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("外見設定")]
    [Tooltip("本体オブジェクト")] public GameObject prefab;
    [Tooltip("本体オブジェクトの位置設定")] public Vector3 initPos;
    [Tooltip("呼出待ちオブジェクト")] public GameObject callingPrefab;

    [Header("呼出設定")]
    [Tooltip("コスト")] public int cost;
    [Tooltip("呼出所要時間（秒）")] public float callTime;

    [Header("ステータス関連")]
    [Tooltip("基本ステータス")] public UnitProfile profile;
    [Tooltip("呼出中ステータス")] public UnitProfile callingProfile;

    protected virtual void Reset()
    {
        callingProfile = new UnitProfile
        {
            id = MapId.Calling,
            unitType = UnitType.Calling,
            unitName = "",
            maxHp = 10.0f,
        };
    }

    private void OnValidate()
    {
        callingProfile.unitName = profile.unitName;
    }
}
