using Cysharp.Threading.Tasks;
using UnityEngine;

public class LinearMover : MonoBehaviour
{
     [Header("移動設定")]
    [Tooltip("スピード"), SerializeField]
    private float _speed = 7.0f;

    private float _elapsedTime = 0f;
    public bool _isAnimating { get; private set; } = false;
    
    public async UniTask MoveToAsync(Vector3 start, Vector3 end)
    {
        // 移動処理中の場合は中止
        if (_isAnimating) return;
        _isAnimating = true;

        // 移動時間の計算
        float duration = Vector3.Distance(start, end) / _speed;
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
}
