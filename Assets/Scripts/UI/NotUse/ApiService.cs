// using System;
// using System.Threading.Tasks;
// using UnityEngine;

// public class ApiService : MonoBehaviour
// {
//     public static ApiService Instance;
//     private GameManager _gameManager;

//     void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     void Start()
//     {
//         ResolveDependencies();
//     }

//     private void ResolveDependencies()
//     {
//         _gameManager = GameManager.Instance;
//     }

//     public async Task PostHoge()
//     {
//         _gameManager.IsLoading = true;

//         try
//         {
//             Debug.Log("API通信開始（イメージ）");
//             await Task.Delay(500); // 現時点では擬似的な通信待機（1秒）
//             Debug.Log("API通信完了（イメージ）");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"処理失敗: {ex.Message}");
//         }
//         finally
//         {
//             _gameManager.IsLoading = false;
//         }
//     }
// }
