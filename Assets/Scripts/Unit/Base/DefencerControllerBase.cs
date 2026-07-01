using System.Collections.Generic;
using UnityEngine;

public abstract class DefencerControllerBase : UnitControllerBase
{
    protected DefencerStatsBase defencerStats => stats as DefencerStatsBase;
    protected DefenceProfile defenceProfile => defencerStats.defenceProfile;

    /// <summary>
    /// 防衛座標リストの取得
    /// </summary>
    public List<Vector2Int> GetDefensiveRangePos(Vector2Int myGridPos)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        switch (defenceProfile.style)
        {
            case DefenceType.Vertical:
                TileRangeUtil.GetForwardVerticalRange(
                    myGridPos,
                    defencerStats.VerticalRange,
                    // Mathf.Min(5, MapManager.Instance.mapHeight - 1 - myGridPos.y),
                    defenceProfile.range.max,
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
        if (defenceProfile.ignoreAccuracy) return true;

        // x座標の差による命中減衰率
        float xOffsetPenaltyRate = defenceProfile.accuracyDecay * distanceX;

        float yOffsetPenaltyRate = (defencerStats.VerticalRange - overlap) * defenceProfile.accuracyDecay;
        float defencerHitRate = defenceProfile.accuracy -  xOffsetPenaltyRate - yOffsetPenaltyRate;
        bool result = attackerEvasionRate < defencerHitRate;

        Debug.Log($"{overlap + 1}回目 => 攻撃側: {attackerEvasionRate} 防衛側: {defencerHitRate} {(result ? "成功" : "失敗")}");

        return result;
    }
}
