using System;
using UnityEngine;

[Serializable]
public struct DefenceProfile
{
    [Tooltip("攻撃力")] public float power;
    [Tooltip("交戦距離")] public AttackRange range;
}

[CreateAssetMenu(fileName = "DefenceUnitData", menuName = "ScriptableObjects/DefenceUnitData")]
public class DefenceUnitData : UnitData
{
    [Tooltip("防衛ステータス")] public DefenceProfile defenceProfile;
}
