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

    public async UniTask ApplyDamageAsync(float power, TileController tile) {
        // Transform tileTransform = tile.transform;
        // 更新前のHPを記録
        float previousHp = _stats.hp;
        // HP更新
        UpdateHp(-power);
        // 攻撃対象へカメラ移動
        // _cameraMovement.SetDestination(new Vector3(tileTransform.position.x, 1, tileTransform.position.z));

        // Explosionパーティクル演出
        // _particlePoolMnager.SpawnParticle(tileTransform.position + Vector3.up, Quaternion.identity);
        // HP変化に応じてダメージ表現
        // if (Mathf.Approximately(previousHp, _stats.hp))
        // {
        //     await _floatingTextPresenter.SpawnDamageAsync(tileTransform, 0);
        // }
        // else
        // {
        //     _animation?.PlayOnce(AnimationName.Hit);
        //     await _floatingTextPresenter.SpawnDamageAsync(tileTransform, power);
        // }

        // HPが0以下の場合、気絶フラグを立てる
        if (_stats.hp <= 0) _stats.IsFaint = true;
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
    public async UniTask OnFaint(TileController tile)
    {
        if (_animation)
        {
            await _animation.PlayOnceAsync(AnimationName.Death);
        }
        TileController tileController = GetComponentInParent<TileController>();
        UnitSpawnManager.Instance.DespawnUnit(tile);
    }
}
