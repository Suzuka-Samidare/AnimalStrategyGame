using UnityEngine;

public class TileController : MonoBehaviour
{    
    // [Header("視認タイマー関連")]
    // [Tooltip("視認時間"), SerializeField]
    // private float revealDuration = 5.0f;
    // private float _timer = 5.0f;

    // void Update()
    // {
    //     UpdateRevealTimer();
    // }

    // public void Reveal()
    // {
    //     // すでに true の場合でも、タイマーを初期値（最大値）にリセットする
    //     isRevealed = true;
    //     _timer = revealDuration;
        
    //     Debug.Log($"{gameObject.name} が表示されました。タイマーリセット。");
    // }

    // private void UpdateRevealTimer()
    // {
    //     // 表示中でない、または一時停止中なら何もしない
    //     if (!isRevealed) return;

    //     // タイマーを減らす
    //     _timer -= Time.deltaTime;

    //     // 0になったら非表示に戻す
    //     if (_timer <= 0)
    //     {
    //         isRevealed = false;
    //         _timer = 0;
    //         Debug.Log($"{gameObject.name} が隠れました。");
    //     }
    // }
}
