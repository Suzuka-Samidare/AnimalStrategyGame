using System;
using UnityEngine;

[Serializable]
public struct AttackProfile
{
    [Tooltip("攻撃力")] public float power;
    [Tooltip("攻撃消費エネルギー")] public int energy;
    [Tooltip("タイムラインへの影響速度")] public float delay;
    [Tooltip("攻撃の種類")] public AttackType style;
    [Tooltip("範囲攻撃の距離")] public AttackRange range;
}

[CreateAssetMenu(fileName = "AttackUnitData", menuName = "ScriptableObjects/AttackUnitData")]
public class AttackUnitData : UnitData
{
    [Tooltip("攻撃ステータス")] public AttackProfile attackProfile;
}
