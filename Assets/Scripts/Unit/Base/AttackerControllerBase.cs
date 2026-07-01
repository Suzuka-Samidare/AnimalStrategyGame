using System.Collections.Generic;
using UnityEngine;

public abstract class AttackerControllerBase : UnitControllerBase
{
    protected AttackerStatsBase attackerStats => stats as AttackerStatsBase;
    protected AttackProfile attackProfile => attackerStats.attackProfile;

    public List<Vector2Int> GetTargetTilePositions(Vector2Int targetPos)
    {
        List<Vector2Int> tilePositions = new List<Vector2Int>();

        // 単体攻撃ならそのマスだけ
        if (attackProfile.style == AttackType.Single)
        {
            tilePositions.Add(targetPos);
            return tilePositions;
        }

        // 範囲攻撃ならTileRangeUtilを使ってリストを埋める
        switch (attackProfile.style)
        {
            case AttackType.Square:
                TileRangeUtil.ForEachSquareRange(targetPos, attackProfile.range.max, 
                    (pos) => tilePositions.Add(pos));
                break;
            case AttackType.Manhattan:
                TileRangeUtil.ForEachManhattanRange(targetPos, attackProfile.range.max, 
                    (pos) => tilePositions.Add(pos));
                break;
            // DEBUG =======================================================
            // case AttackType.Test:
            //     TileRangeUtil.GetForwardVerticalRange(
            //         targetPos,
            //         MapManager.Instance.mapHeight - 1 - targetPos.y,
            //         1,
            //         (pos) => tilePositions.Add(pos)
            //     );
            //     break;
            // DEBUG =======================================================

        }

        return tilePositions;
    }
}
