using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DefenceUnitStats))]
public class DefenceUnitController : MonoBehaviour
{
    [Header("ステータス")]
    public readonly int VerticalRange = 5;

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
                    VerticalRange,
                    // Mathf.Min(5, MapManager.Instance.mapHeight - 1 - myGridPos.y),
                    _defenceProfile.range.max,
                    (pos) => tiles.Add(pos)
                );
                break;
        }

        return tiles;
    }

    /// <summary>
    /// 防衛判定結果
    /// </summary>
    public bool IsIntercepted(float attackerEvasionRate, int overlap, float distanceX)
    {
        if (_defenceProfile.ignoreAccuracy) return true;

        // x座標の差による命中減衰率
        float xOffsetPenaltyRate = _defenceProfile.accuracyDecay * distanceX;

        float yOffsetPenaltyRate = (VerticalRange - overlap) * _defenceProfile.accuracyDecay;
        float defencerHitRate = _defenceProfile.accuracy -  xOffsetPenaltyRate - yOffsetPenaltyRate;
        bool result = attackerEvasionRate < defencerHitRate;

        Debug.Log($"{overlap + 1}回目 => 攻撃側: {attackerEvasionRate} 防衛側: {defencerHitRate} {(result ? "成功" : "失敗")}");

        return result;
    }
}
