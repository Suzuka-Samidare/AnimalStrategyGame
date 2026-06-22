using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class ParabolicMover : MonoBehaviour
{
    [System.Serializable]
    public struct MovementPath
    {
        public Vector3 start;  // 始点
        public Vector3 end;   // 頂点
    }

    [Header("【上昇フェーズ】設定")]
    [Tooltip("上昇座標情報"), SerializeField]
    private MovementPath _ascentPath = new MovementPath {
        start = Vector3.zero,
        end =  Vector3.zero,
    };
    [Tooltip("上昇時間"), SerializeField]
    private float _ascentDuration = 1.0f;

    [Header("【下降フェーズ】設定")]
    [Tooltip("下降座標情報"), SerializeField]
    private MovementPath _descentPath = new MovementPath {
        start =  Vector3.zero,
        end =  Vector3.zero,
    };
    [Tooltip("下降時間"), SerializeField]
    private float _descentDuration = 1.0f;

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
        if (_isAnimating) return;
        _isAnimating = true;

        _ascentPath = ascentPath;
        // 上昇時間の算出
        _ascentDuration = Mathf.Abs(_ascentPath.start.z - _ascentPath.end.z) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = _ascentPath.start;
        // 上昇持続時間中はオブジェクト移動
        while (_elapsedTime < _ascentDuration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / _ascentDuration);
            
            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(_ascentPath.start.x, _ascentPath.end.x, t);
            float currentZ = Mathf.Lerp(_ascentPath.start.z, _ascentPath.end.z, t);
            // 垂直（Y）の計算：Sinを使って頂点に向けて滑らかに減速
            float currentY = Mathf.Lerp(_ascentPath.start.y, _ascentPath.end.y, Mathf.Sin(t * Mathf.PI / 2));
            // 放物線に沿って移動
            transform.position = new Vector3(currentX, currentY, currentZ);

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // オブジェクトを終着地点に合わせる
        transform.position = _ascentPath.end;

        _isAnimating = false;
    }

    /// <summary>
    /// 下降移動
    /// </summary>
    public async UniTask DescentAsync(MovementPath descentPath)
    {
        Debug.Log("[[[DescentAsync]]]");

        if (_isAnimating) return;
        _isAnimating = true;

        _descentPath = descentPath;
        // 下降時間の算出
        _descentDuration = Mathf.Abs(_descentPath.start.z - _descentPath.end.z) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = _descentPath.start;
        // 下降持続時間中はオブジェクト移動
        while (_elapsedTime < _descentDuration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / _descentDuration);

            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(_descentPath.start.x, _descentPath.end.x, t);
            float currentZ = Mathf.Lerp(_descentPath.start.z, _descentPath.end.z, t);
            // 垂直（Y）の計算：Cosを使って頂点から滑らかに加速しながら落下
            float currentY = Mathf.Lerp(_descentPath.start.y, _descentPath.end.y, 1 - Mathf.Cos(t * Mathf.PI / 2));
            // 放物線に沿って移動
            transform.position = new Vector3(currentX, currentY, currentZ);

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        // オブジェクトを終着地点に合わせる
        transform.position = _descentPath.end;

        _isAnimating = false;
        Destroy(gameObject);
    }

    /// <summary>
    /// 下降移動 + 途中でアクション
    /// </summary>
    public async UniTask DescentWithInterruptAsync(MovementPath descentPath, Vector3 interceptedPos, Func<Vector3, UniTask> interruptAction )
    {
        Debug.Log("[[[DescentWithInterruptAsync]]]");

        if (_isAnimating) return;
        _isAnimating = true;

        _descentPath = descentPath;
        // 下降時間の算出
        _descentDuration = Mathf.Abs(_descentPath.start.z - _descentPath.end.z) / _speed;
        // 時間をリセット
        _elapsedTime = 0f;
        // オブジェクトを開始地点に合わせる
        transform.position = _descentPath.start;
        // 下降持続時間中はオブジェクト移動
        while (_elapsedTime < _descentDuration)
        {
            // 経過時間の加算
            _elapsedTime += Time.deltaTime;
            // 進行度 (0.0 ～ 1.0)
            float t = Mathf.Clamp01(_elapsedTime / _descentDuration);

            // 水平（X, Z）の補間
            float currentX = Mathf.Lerp(_descentPath.start.x, _descentPath.end.x, t);
            float currentZ = Mathf.Lerp(_descentPath.start.z, _descentPath.end.z, t);
            // 垂直（Y）の計算：Cosを使って頂点から滑らかに加速しながら落下
            float currentY = Mathf.Lerp(_descentPath.start.y, _descentPath.end.y, 1 - Mathf.Cos(t * Mathf.PI / 2));
            // 最終的な座標
            Vector3 currentPos = new Vector3(currentX, currentY, currentZ);
            // 放物線に沿って移動
            transform.position = currentPos;

            // TODO: 移動速度が速いと正確に判定が動かないので、別の方法が無いか考える
            if (Mathf.Abs(currentZ - interceptedPos.z) < 0.1) {
                Destroy(gameObject);
                await interruptAction(currentPos);
                break;
            }

            // 次のフレームのUpdateタイミングまで待機する
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _isAnimating = false;
        // Destroy(gameObject);
    }
}
