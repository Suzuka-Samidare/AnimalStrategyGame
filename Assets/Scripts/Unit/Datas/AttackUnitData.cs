using System;
using UnityEngine;

public enum ProjectileType
{
    Ink,
    HerringSchool,
    Drone
}

[Serializable]
public struct AttackProfile
{
    [Tooltip("攻撃力")] public float power;
    [Tooltip("攻撃消費エネルギー")] public int energy;
    [Tooltip("タイムラインへの影響速度")] public float delay;
    [Tooltip("攻撃の種類")] public AttackType style;
    [Tooltip("範囲攻撃の距離")] public AttackRange range;
}

[Serializable]
public struct ProjectileProfile
{
    [Tooltip("発射物タイプ")] public ProjectileType projectileType;
    [Tooltip("最大耐久値")] public float maxHp;
}

[CreateAssetMenu(fileName = "AttackUnitData", menuName = "ScriptableObjects/AttackUnitData")]
public class AttackUnitData : UnitData
{
    [Tooltip("攻撃ステータス")] public AttackProfile attackProfile;
    [Tooltip("基本ステータス")] public ProjectileProfile projectileProfile;
}
