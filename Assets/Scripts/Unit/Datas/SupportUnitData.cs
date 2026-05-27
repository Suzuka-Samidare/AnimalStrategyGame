using System;
using UnityEngine;

[Serializable]
public struct SupportProfile
{
    [Tooltip("サポート力")] public float power;
}

[CreateAssetMenu(fileName = "SupportUnitData", menuName = "ScriptableObjects/SupportUnitData")]
public class SupportUnitData : UnitData
{
    [Tooltip("防衛ステータス")] public SupportProfile supportProfile;
}

