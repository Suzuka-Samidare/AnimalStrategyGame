using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FloatingTextPresenter : MonoBehaviour
{
    public static FloatingTextPresenter Instance { get; private set; }

    [Header("Settings")]
    public GameObject Prefab;
    public Transform CanvasTransform;

    [Header("Color Palette")]
    public Color damageColor = Color.red;     // インスペクターで好きな赤を選んでね！
    public Color recoveryColor = Color.green; // インスペクターで好きな緑を！

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ダメージ用
    public async UniTask SpawnDamageAsync(Vector3 position, float amount)
    {
        await CreateTextAsync(position, amount, damageColor);
    }
    // 回復用
    public async UniTask SpawnRecoveryAsync(Vector3 position, float amount)
    {
        await CreateTextAsync(position, amount, recoveryColor);
    }

    // 共通の生成処理
    private async UniTask CreateTextAsync(Vector3 position, float amount, Color color)
    {
        GameObject floatingText = Instantiate(Prefab, CanvasTransform);
        FloatingTextView floatingTextView = floatingText.GetComponent<FloatingTextView>();
        await floatingTextView.SetupAsync(position, amount, color);
    }
}
