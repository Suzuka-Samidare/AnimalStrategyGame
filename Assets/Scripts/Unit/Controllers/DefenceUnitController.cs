using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DefenceUnitStats))]
public class DefenceUnitController : MonoBehaviour
{
    [Header("ステータス")]
    private int _verticalRange = 5;

    [Header("Refs")]
    private DefenceProfile _defenceProfile;

    private void Start()
    {
        _defenceProfile = GetComponent<DefenceUnitStats>().profile;
    }

    /// <summary>
    /// 防衛座標リストの取得
    /// </summary>
    public List<Vector2Int> GetDefensiveRangePos(Vector2Int myGridPos)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        switch (_defenceProfile.style)
        {
            case DefenceType.Vertical:
                TileRangeUtil.GetForwardVerticalRange(
                    myGridPos,
                    _verticalRange,
                    // Mathf.Min(5, MapManager.Instance.mapHeight - 1 - myGridPos.y),
                    _defenceProfile.range.max,
                    (pos) => tiles.Add(pos)
                );
                break;
        }

        return tiles;
    }

    /// <summary>
    /// 防衛可能最大タイル数
    /// </summary>
    // public int GetMaxDefendableTiles()
    // {
    //     switch (_defenceProfile.style)
    //     {
    //         case DefenceType.Vertical:
    //             return (1 + _defenceProfile.range.max * 2) * _verticalRange;
    //     }

    //     throw new System.Exception("合致するDefenceTypeがありませんでした。");
    // }

    /// <summary>
    /// 防衛判定結果
    /// </summary>
    public bool IsIntercepted(int overlapCount, float distanceX)
    {
        if (_defenceProfile.ignoreAccuracy) return true;

        // x座標の差による命中減衰率
        float xOffsetPenaltyRate = _defenceProfile.accuracyDecay * distanceX;
        float attackerHitRate = Random.value;

        for (int i = 0; i < overlapCount; i++)
        {
            float yOffsetPenaltyRate = (_verticalRange - i) * _defenceProfile.accuracyDecay;
            float defencerHitRate = _defenceProfile.accuracy -  xOffsetPenaltyRate - yOffsetPenaltyRate;
            bool result = attackerHitRate < defencerHitRate;

            Debug.Log($"{i+1}回目 => 攻撃側: {attackerHitRate} 防衛側: {defencerHitRate} {(result ? "成功" : "失敗")}");
            if (result) return true;
        }

        return false;
    }
}
