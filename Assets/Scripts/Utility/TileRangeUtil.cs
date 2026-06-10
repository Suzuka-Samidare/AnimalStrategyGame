using System;
using System.Collections.Generic;
using UnityEngine;

public static class TileRangeUtil
{
    public static void ForEachSquareRange(Vector2Int tgt, int range, Action<Vector2Int> action)
    {
        for (int y = tgt.y - range; y <= tgt.y + range; y++)
        {
            for (int x = tgt.x - range; x <= tgt.x + range; x++)
            {
                // 座標を引数として設定して外部からDoSomething
                action(new Vector2Int(x, y));
            }
        }
    }

    public static void ForEachManhattanRange(Vector2Int tgt, int range, Action<Vector2Int> action)
    {
        for (int y = tgt.y - range; y <= tgt.y + range; y++)
        {
            // Y軸方向の距離を計算
            int distY = Mathf.Abs(y - tgt.y);
            // 残りの移動可能距離（range - distY）がX軸に割ける最大幅になる
            int xRemaining = range - distY;

            for (int x = tgt.x - xRemaining; x <= tgt.x + xRemaining; x++)
            {
                // 座標を引数として設定して外部からDoSomething
                action(new Vector2Int(x, y));
            }
        }
    }

    public static void GetForwardVerticalRange(Vector2Int pos, int verticalRange, int horizonSpread, Action<Vector2Int> action)
    {
        // for (int y = pos.y; y <= pos.y + verticalRange; y++)
        int frontPos = pos.y + 1;
        for (int y = frontPos; y < frontPos + verticalRange; y++)
        {
            for (int x = pos.x - horizonSpread; x <= pos.x + horizonSpread; x++)
            {
                action(new Vector2Int(x, y));
            }
        }
    }

    // public static void ForEachSquareRange(int centerY, int centerX, int range, Action<int, int> action)
    // {
    //     for (int y = centerY - range; y <= centerY + range; y++)
    //     {
    //         for (int x = centerX - range; x <= centerX + range; x++)
    //         {
    //             // 座標を引数として設定して外部からDoSomething
    //             action(y, x);
    //         }
    //     }
    // }

    // public static void ForEachManhattanRange(int centerY, int centerX, int range, Action<int, int> action)
    // {
    //     for (int y = centerY - range; y <= centerY + range; y++)
    //     {
    //         // Y軸方向の距離を計算
    //         int distY = Mathf.Abs(y - centerY);
    //         // 残りの移動可能距離（range - distY）がX軸に割ける最大幅になる
    //         int xRemaining = range - distY;

    //         for (int x = centerX - xRemaining; x <= centerX + xRemaining; x++)
    //         {
    //             // 座標を引数として設定して外部からDoSomething
    //             action(y, x);
    //         }
    //     }
    // }
}
