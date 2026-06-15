using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class SquidController : MonoBehaviour
{
    [SerializeField]
    private Vector3 launchPos;
    [SerializeField]
    private Vector3 tgtPos;
    [SerializeField]
    private float speed;
    private float duration;
    private float maxHeight;
    private CancellationTokenSource cts;

    [Header("Refs")]
    private UnitAnimation _unitAnimation;

    private void Awake()
    {
        _unitAnimation = GetComponent<UnitAnimation>();
    }

    // [ContextMenu("じっけん！")]
    // public void TestestFunc()
    // {
    //     Debug.Log("====== TestestFunc =================");

    //     LerpLaunch();
    // }

    public async UniTask LerpLaunch(Vector3 finishPos)
    {
        launchPos = transform.position;
        tgtPos = finishPos; // DEBUG
        // 2点間の距離
        float distance = Vector3.Distance(launchPos, tgtPos);
        // 飛翔時間
        duration = distance / speed;
        // 高さの最高点
        maxHeight = distance * 0.25f;
        // キャンセラレーショントークンのインスタンスを生成
        cts = new CancellationTokenSource();
        // 攻撃前のアニメーション
        _unitAnimation.PlayOnce(AnimationName.Attack);

        await FlyAsync(maxHeight, cts.Token);
    }

    private async UniTask FlyAsync(float maxHeight, CancellationToken token)
    {
        float time = 0f;

        // 1フレームごとにオブジェクトを移動する
        while (time < duration)
        {
            // ゲームオブジェクトがDestroyされた場合は処理終了
            if (token.IsCancellationRequested) return;

            time += Time.deltaTime;
            float t = time / duration;

            // 放物線の座標計算
            Vector3 currentPos = Vector3.Lerp(launchPos, tgtPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * maxHeight;
            currentPos.y += height;

            transform.position = currentPos;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        transform.position = tgtPos;
        // Debug.Log("FINISH FlyAsync");
        transform.position = launchPos;
    }
}
