using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Refs")]
    private UnitStats _stats;
    private UnitProfile _profile;
    private AttackUnitController _attackUnitController;
    private UnitAnimation _animation;
    private AttackManager _attackManager;
    private MapManager _mapManager;
    private ParticlePoolManager _particlePoolMnager;
    private CameraMovement _cameraMovement;
    private FloatingTextPresenter _floatingTextPresenter;


    private void Start()
    {
        _stats = GetComponent<UnitStats>();
        _profile = _stats.profile;
        _animation = GetComponent<UnitAnimation>();
        _attackManager = AttackManager.Instance;
        _mapManager = MapManager.Instance;
        _particlePoolMnager = ParticlePoolManager.Instance;
        _cameraMovement = CameraMovement.Instance;
        _floatingTextPresenter = FloatingTextPresenter.Instance;

        if (TryGetComponent<AttackUnitController>(out var attackUnitController))
        {
            _attackUnitController = attackUnitController;
        }
    }

    public async Task ApplyDamageAsync(float power, Transform tileTransform) {
        // 更新前のHPを記録
        float previousHp = _stats.hp;
        // HP更新
        UpdateHp(-power);
        // 攻撃対象へカメラ移動
        _cameraMovement.SetDestination(new Vector3(tileTransform.position.x, 1, tileTransform.position.z));
        // Explosionパーティクル演出
        _particlePoolMnager.SpawnParticle(tileTransform.position + Vector3.up, Quaternion.identity);
        // HP変化に応じてダメージ表現
        if (Mathf.Approximately(previousHp, _stats.hp))
        {
            await _floatingTextPresenter.SpawnDamageAsync(tileTransform, 0);
        }
        else
        {
            _animation?.PlayOnce(AnimationName.Hit);
            await _floatingTextPresenter.SpawnDamageAsync(tileTransform, power);
        }
        // HPが0の場合、気絶処理を実行
        if (_stats.hp <= 0) await OnFaint();
    }

    // public async UniTask ApplyHealAsync(float heal, Transform tileTransform)
    // {
    //     UpdateHp(heal);
    // }

    /// <summary>
    /// HP更新（ダメージ、回復）
    /// </summary>
    private void UpdateHp(float amount)
    {
        // HPの増減計算
        _stats.hp = Mathf.Clamp(_stats.hp + amount, 0, _stats.profile.maxHp);
    }

    // private async UniTask OnDamage() {
    //     await _animation.PlayOnceAsync(AnimationName.Hit);
    //     Debug.Log("痛いっ！エフェクト出すよ！");
    // }

    // private async UniTask OnHeal() {
    //     await _animation.PlayOnceAsync(AnimationName.Bounce);
    //     Debug.Log("回復！キラキラさせるよ！");
    // }
    
    /// <summary>
    /// 気絶処理
    /// </summary>
    private async UniTask OnFaint()
    {
        await _animation.PlayOnceAsync(AnimationName.Death);
        TileController tileController = GetComponentInParent<TileController>();
        tileController.DestroyUnit();
    }
    
    // public void ApplyHeal(float healAmount) => UpdateHp(healAmount);

    // public void ExecuteAttack(int centerY, int centerX)
    // {
    //     UnitProfile profile = _stats.profile;

    //     if (!profile.canAttack) return; // 攻撃不可なら即リターン

    //     if (profile.isAreaAttack)
    //     {
    //         // 範囲攻撃：汎用メソッドに Action を渡して実行！
    //         TileRangeUtil.ForEachSquareRange(centerY, centerX, profile.areaAttackRange, (y, x) =>
    //         {
    //             ApplyDamage(centerY, centerX, profile.power);
    //         });
    //     }
    //     else
    //     {
    //         // 単体攻撃
    //         ApplyDamage(centerY, centerX, profile.power);
    //     }
    // }

    // 攻撃を実行（予約）するメソッド
    // public void EnqueueAttackRequest(Vector2Int targetPos)
    // {
    //     if (!_profile.canAttack) {
    //         Debug.Log("攻撃可能ユニットではありません");
    //         return;
    //     }

    //     // 1. 形状と範囲に基づいて攻撃対象タイルをリストアップ
    //     List<Vector2Int> tilePositions = GetTargetTilePositions(targetPos);
    //     List<TileController> tileControllers = new List<TileController>();

    //     foreach (Vector2Int pos in tilePositions)
    //     {
    //         TileController tileController = _mapManager.GetEnemyMapTile(pos);
    //         if (tileController != null)
    //         {
    //             tileControllers.Add(tileController);
    //         }
    //     }

    //     // 2. 攻撃キューに登録（前回作ったManagerへ）
    //     // delayは攻撃アニメーションの着弾時間とかを想定
    //     AttackCommand newAttack = new AttackCommand(tileControllers, _profile.power, 3.0f);
    //     _attackManager.attackQueue.Enqueue(newAttack);

    //     Debug.Log($"{_profile.unitName} が {_profile.atkType} 範囲で攻撃予約！ ✨");
    // }
}
