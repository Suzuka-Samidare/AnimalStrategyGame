using System;
using UnityEditor.EditorTools;
using UnityEngine;

public enum DefenceType
{
    Single,     // 単体
    Vertical,
}

[Serializable]
public struct DefenceProfile
{
    [Tooltip("攻撃力")] public float power;
    [Tooltip("守備種類")] public DefenceType style;
    [Tooltip("守備距離")] public AttackRange range;
    [Tooltip("命中率計算の有無")] public bool ignoreAccuracy;
    [Tooltip("命中精度")] public float accuracy;
    [Tooltip("命中減衰率")] public float accuracyDecay;
}

[CreateAssetMenu(fileName = "DefenceUnitData", menuName = "ScriptableObjects/DefenceUnitData")]
public class DefenceUnitData : UnitData
{
    [Tooltip("防衛ステータス")] public DefenceProfile defenceProfile;
}
