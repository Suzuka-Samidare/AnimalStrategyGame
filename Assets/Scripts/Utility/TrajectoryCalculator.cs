using UnityEngine;

[System.Serializable]
public struct MovementPath
{
    public Vector3 start;  // 始点
    public Vector3 end;   // 頂点
}

public struct InterceptTargetInfo
{
    public Vector3 Position;     // 計算されたYを含む、正確な迎撃座標
    public float TimeToReach;    // 開始地点からその座標に到達するまでの秒数
}

public static class TrajectoryCalculator
{
    /// <summary>
    /// 指定されたZ座標を元に、軌道上の正確な座標と到達時間を逆算する
    /// </summary>
    public static InterceptTargetInfo CalculateDescentInterceptInfo(
        MovementPath descentPath, 
        float speed, 
        float targetZ)
    {
        // 1. 全体の下降持続時間を算出
        float duration = Mathf.Abs(descentPath.start.z - descentPath.end.z) / speed;

        // 2. 指定されたZ座標が、始点から終点までのどの割合(0.0 〜 1.0)に位置するかを逆算 (t)
        float t = Mathf.InverseLerp(descentPath.start.z, descentPath.end.z, targetZ);
        t = Mathf.Clamp01(t);

        // 3. 経過時間を逆算
        float timeToReach = duration * t;

        // 4. 水平Xの補間
        float currentX = Mathf.Lerp(descentPath.start.x, descentPath.end.x, t);

        // 5. 垂直Yの計算（現在の移動ロジックと同じ数式を適用）
        float currentY = Mathf.Lerp(descentPath.start.y, descentPath.end.y, 1f - Mathf.Cos(t * Mathf.PI / 2f));

        // 6. 結果をまとめて返す
        return new InterceptTargetInfo
        {
            Position = new Vector3(currentX, currentY, targetZ),
            TimeToReach = timeToReach
        };
    }
}