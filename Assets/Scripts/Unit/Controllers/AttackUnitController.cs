using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttackUnitStats))]
public class AttackUnitController : MonoBehaviour
{
    private AttackProfile _attackProfile;

    private void Start()
    {
        _attackProfile = GetComponent<AttackUnitStats>().profile;
    }

    public List<Vector2Int> GetTargetTilePositions(Vector2Int targetPos)
    {
        List<Vector2Int> tilePositions = new List<Vector2Int>();

        // 単体攻撃ならそのマスだけ
        if (_attackProfile.style == AttackType.Single)
        {
            tilePositions.Add(targetPos);
            return tilePositions;
        }

        // 範囲攻撃ならTileRangeUtilを使ってリストを埋める
        switch (_attackProfile.style)
        {
            case AttackType.Square:
                TileRangeUtil.ForEachSquareRange(targetPos, _attackProfile.range.max, 
                    (pos) => tilePositions.Add(pos));
                break;
            case AttackType.Manhattan:
                TileRangeUtil.ForEachManhattanRange(targetPos, _attackProfile.range.max, 
                    (pos) => tilePositions.Add(pos));
                break;
            case AttackType.Cross:
                // 十字範囲が必要ならここにUtilを追加して呼ぶ感じ！
                break;
        }

        return tilePositions;
    }
}
