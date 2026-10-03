using Cysharp.Threading.Tasks;
using UnityEngine;

public class LinearMover : MonoBehaviour
{
    // [Header("移動設定")]
    // [Tooltip("スピード"), SerializeField]
    private float _moveSpeed = 0.5f;
    private float _rotationSpeed = 2.0f;
    [SerializeField] private float _facingThresholdDot = 0.99f; // 正面判定の閾値（0.99 ≒ 約8度以内）

    private float _elapsedTime = 0f;
    public bool _isAnimating { get; private set; } = false;
    
    public async UniTask MoveToAsync(Vector3 start, Vector3 end, float speed = 7.0f)
    {
        // 移動処理中の場合は中止
        if (_isAnimating) return;
        _isAnimating = true;

        // 移動時間の計算
        float duration = Vector3.Distance(start, end) / speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = start;
        // 上昇持続時間中はオブジェクト移動
        while (_elapsedTime < duration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / duration);
            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(start.x, end.x, t);
            float currentZ = Mathf.Lerp(start.z, end.z, t);
            // 垂直（Y）の計算：Sinを使って頂点に向けて滑らかに減速
            float currentY = Mathf.Lerp(start.y, end.y, Mathf.Sin(t * Mathf.PI / 2));
            // 放物線に沿って移動
            transform.position = new Vector3(currentX, currentY, currentZ);

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // オブジェクトを終着地点に合わせる
        transform.position = end;
        // フラグ解除
        _isAnimating = false;
    }

    /// <summary>
    /// 指定された方向（正規化済み）に向かって回転する
    /// </summary>
    public bool RotateTowards(Vector3 offset)
    {
        // 1. 方向ベクトルがゼロ（停止時）の場合は理を行わない
        if (offset == Vector3.zero) return false;

        // XZ平面の移動に対応するため Y 軸の変位をリセット（斜め下を向くのを防ぐ）
        Vector3 direction = offset;
        direction.y = 0;
        direction.Normalize();

        // 2. 移動方向を向く回転（Quaternion）を計算し、スムーズ回転させる
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            _rotationSpeed * Time.deltaTime
        );

        // 3. 現在の正面ベクトル（transform.forward）と移動方向（direction）の一致度を計算
        float dot = Vector3.Dot(transform.forward, direction);

        // Debug.Log($"dot: {dot}  _facingThresholdDot: {_facingThresholdDot}");

        return dot >= _facingThresholdDot;
    }

    /// <summary>
    /// 指定された方向（正規化済み）へ 1フレーム分移動する
    /// </summary>
    public void MoveInDirection(Vector3 offset)
    {
        if (offset == Vector3.zero) return;

        // XZ平面の移動に対応するため Y 軸の変位をリセット（斜め下を向くのを防ぐ）
        Vector3 direction = offset;
        direction.y = 0;
        direction.Normalize();

        transform.position += direction * (_moveSpeed * Time.deltaTime);
    }
}
