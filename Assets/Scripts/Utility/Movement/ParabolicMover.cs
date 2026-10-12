using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class ParabolicMover : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("スピード"), SerializeField]
    private float _speed = 7.0f;

    private float _elapsedTime = 0f;
    public bool _isAnimating { get; private set; } = false;

    /// <summary>
    /// 上昇移動
    /// </summary>
    public async UniTask AscendAsync(MovementPath ascentPath)
    {
        // Debug.Log("[[[AscendAsync]]]");

        // 移動処理中の場合は中止
        if (_isAnimating) return;
        _isAnimating = true;

        // 移動時間の計算
        float duration = Vector3.Distance(ascentPath.start, ascentPath.end) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = ascentPath.start;
        // 上昇持続時間中はオブジェクト移動
        while (_elapsedTime < duration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / duration);
            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(ascentPath.start.x, ascentPath.end.x, t);
            float currentZ = Mathf.Lerp(ascentPath.start.z, ascentPath.end.z, t);
            // 垂直（Y）の計算：Sinを使って頂点に向けて滑らかに減速
            float currentY = Mathf.Lerp(ascentPath.start.y, ascentPath.end.y, Mathf.Sin(t * Mathf.PI / 2));
            // 放物線に沿って移動
            transform.position = new Vector3(currentX, currentY, currentZ);

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // オブジェクトを終着地点に合わせる
        transform.position = ascentPath.end;
        // フラグ解除
        _isAnimating = false;
    }

    /// <summary>
    /// 下降移動
    /// </summary>
    public async UniTask DescentAsync(MovementPath descentPath)
    {
        // Debug.Log("[[[DescentAsync]]]");

        if (_isAnimating) return;
        _isAnimating = true;

        // 下降時間の算出
        float duration = Mathf.Abs(descentPath.start.z - descentPath.end.z) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = descentPath.start;
        // 下降持続時間中はオブジェクト移動
        while (_elapsedTime < duration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / duration);

            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(descentPath.start.x, descentPath.end.x, t);
            float currentZ = Mathf.Lerp(descentPath.start.z, descentPath.end.z, t);
            // 垂直（Y）の計算：Cosを使って頂点から滑らかに加速しながら落下
            float currentY = Mathf.Lerp(descentPath.start.y, descentPath.end.y, 1 - Mathf.Cos(t * Mathf.PI / 2));
            // 最終的な座標
            Vector3 currentPos = new Vector3(currentX, currentY, currentZ);
            // 放物線に沿って移動
            transform.position = currentPos;

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // オブジェクトを終着地点に合わせる
        transform.position = descentPath.end;

        _isAnimating = false;
    }

    /// <summary>
    /// 下降移動 + 途中でアクション
    /// </summary>
    public async UniTask DescentWithInterruptAsync(
        MovementPath descentPath,
        InterceptTargetInfo interceptInfo,
        Func<Vector3, UniTask> onBlockedAction)
    {
        // Debug.Log("[[[DescentWithInterruptAsync]]]");

        if (_isAnimating) return;
        _isAnimating = true;

        // 下降時間の算出
        float duration = Mathf.Abs(descentPath.start.z - descentPath.end.z) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = descentPath.start;
        // 迎撃
        // bool isInterceptStarted = false;
        // 下降持続時間中はオブジェクト移動
        while (_elapsedTime < duration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / duration);

            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(descentPath.start.x, descentPath.end.x, t);
            float currentZ = Mathf.Lerp(descentPath.start.z, descentPath.end.z, t);
            // 垂直（Y）の計算：Cosを使って頂点から滑らかに加速しながら落下
            float currentY = Mathf.Lerp(descentPath.start.y, descentPath.end.y, 1 - Mathf.Cos(t * Mathf.PI / 2));
            // 最終的な座標
            Vector3 currentPos = new Vector3(currentX, currentY, currentZ);
            // 放物線に沿って移動
            transform.position = currentPos;

            // TODO: 移動速度が速いと正確に判定が動かないので、別の方法が無いか考える
            if (_elapsedTime >= interceptInfo.TimeToReach)
            {
                transform.position = interceptInfo.Position;
                await onBlockedAction(currentPos);
                break;
            }

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _isAnimating = false;
    }
}
