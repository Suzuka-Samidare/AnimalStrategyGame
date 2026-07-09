using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TimelineCommand = TimelineManager.TimelineCommand;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private CombatPerformanceDirector _combatPerformanceDirector;
    private MapManager _mapManager;
    private ParticleManager _particleManager;

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

    private void Start()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _particleManager = ParticleManager.Instance;
    }

    /// <summary>
    /// 影響タイル（ユニット）へのダメージ反映
    /// </summary>
    public void ApplyDamage(TimelineCommand command)
    {
        foreach (Tile tile in command.AffectedTiles)
        {
            if (tile.IsExistUnit)
            {
                tile.Unit.Stats.ApplyDamageAsync(command.Damage, tile);
                // UniTask damageTask = tile.UnitBase.Controller.ApplyDamageAsync(command.Damage, tile);
                // // あとで一括待機するためにリストに入れておく
                // applyTask.Add(damageTask);
            }
            else
            {
                // TODO: MISS表記を入れたい -> ユニットによる位置基準ではダメ
                // TODO: MISS表記しない場合 -> ACTION中に攻撃範囲を分かるようにしたい
                Debug.Log("ダメージを与えるユニットが、このタイルにはいません。");
            }
        }
    }

    /// <summary>
    /// インク迎撃プロセス
    /// </summary>
    // TODO: ProcessInkAttackに名前を変えたい
    public async UniTask ProcessInkInterceptAttempt(TimelineCommand command)
    {
        // 防衛が成功したか
        bool isSuccessDefence;
        // 迎撃したユニットのタイル（いなければnull）
        Tile interceptingUnitTile;
        // 迎撃完了座標（グリッド）
        Vector2Int interceptGridPos;
        // 迎撃結果の取得
        isSuccessDefence = GetHerringDefenceResult(command, out interceptingUnitTile, out interceptGridPos);
        Vector3 interceptedPos = command.Owner == Owner.Player
            ? _mapManager.GetEnemyTile(interceptGridPos, true).Stats.GlobalPos
            : _mapManager.GetPlayerTile(interceptGridPos, true).Stats.GlobalPos;

        Debug.Log($"isSuccessDefence: {isSuccessDefence}");
        // 防衛に失敗している場合、内部的なダメージの反映（見た目に反映されないAPI通信に近い更新）
        if (!isSuccessDefence)
        {
            ApplyDamage(command);
        }

        // ===================================================
        // 見た目の演出
        // ===================================================
        // 防錆成功 => 攻撃が迎撃される演出のみ
        // 防衛失敗 => 攻撃ヒット及びユニットの気絶演出
        if (isSuccessDefence)
        {
            await _combatPerformanceDirector.AttackInkFailed(command, interceptingUnitTile.Stats.GlobalPos, interceptedPos);
        }
        else
        {
            await _combatPerformanceDirector.AttackInkSuccess(command);
        }

        // 防衛に失敗している場合は、攻撃ヒット及びユニットの気絶演出
        if (!isSuccessDefence)
        {
            await AttackHitEffects(command);
            await FaintEffects(command);
        }
    }

    public async UniTask AttackHitEffects(TimelineCommand command)
    {
        List<UniTask> animationTasks = new List<UniTask>();
        foreach (Tile tile in command.AffectedTiles)
        {
            // 爆発のパーティクルを生成
            UniTask explosion = _particleManager.PerformFireExplosionAsync(tile.transform.position + Vector3.up, Quaternion.identity);
            animationTasks.Add(explosion);
            // ユニットがいない場合はここで処理終了
            if (!tile.IsExistUnit) continue;
            // ダメージ表示
            UniTask damageText = FloatingTextPresenter.Instance.SpawnDamageAsync(tile.Stats.GlobalPos, command.Damage);
            animationTasks.Add(damageText);
        }
        await UniTask.WhenAll(animationTasks.ToArray());
    }


    public async UniTask FaintEffects(TimelineCommand command)
    {
        List<UniTask> animationTasks = new List<UniTask>();
        foreach (Tile tile in command.AffectedTiles)
        {
            // 気絶している場合は、アニメーション
            if (tile.Unit != null && tile.Unit.Stats.IsFaint) {
                UniTask faint = tile.Unit.OnFaint(tile);
                animationTasks.Add(faint);
            }
        }
        await UniTask.WhenAll(animationTasks.ToArray());
    }

    /// <summary>
    /// 攻撃に対する防衛判定
    /// </summary>
    public bool GetHerringDefenceResult(TimelineCommand command, out Tile interceptingUnitTile, out Vector2Int interceptGridPos)
    {
        // ターゲットの座標
        Vector2Int tgtPos = command.TargetTile.Stats.GridPos;
        // 攻撃が着弾するまでに通過するタイル
        List<Tile> trajectoryTiles = _mapManager.GetTrajectoryTiles(command.TargetTile);
        // Herringユニットがいるタイル
        List<Tile> herringTiles = command.Owner == Owner.Player
            ? _mapManager.GetEnemyMapHerringTiles()
            : _mapManager.GetPlayerMapHerringTiles();
        // y座標値が高い順に並べ替え
        herringTiles.Sort((a, b) => b.Stats.GridPos.y.CompareTo(a.Stats.GridPos.y));

        // Debug.Log($"Herringユニットの総数: {herringTiles.Count}");

        // 各Herringユニットごとに防衛処理
        foreach (var herringTile in herringTiles)
        {
            HerringUnit herringUnit = herringTile.Unit as HerringUnit;
            // Herringユニットの左右防衛幅
            int horizonRange = herringUnit.Stats.defenceProfile.range.max;
            // ターゲットのx座標がHerringユニットの防衛幅に入らない場合はスキップ
            if (tgtPos.x < herringTile.Stats.GridPos.x - horizonRange || tgtPos.x > herringTile.Stats.GridPos.x + horizonRange) continue;

            // Herringユニットの防衛座標リストを取得
            List<Vector2Int> defencePositions = herringUnit.Controller.GetDefensiveRangePos(herringTile.Stats.GridPos);
            // 有効な防衛タイル数の集計
            int overlapCount = 0;

            // ====================================================
            // マップ内の防衛座標の集計
            // ====================================================
            // マップ内で有効な防衛座標リストを取得
            List<Tile> defenceTiles = herringTile.Stats.owner == Owner.Player
                ? _mapManager.GetPlayerTiles(defencePositions, true)
                : _mapManager.GetEnemyTiles(defencePositions, true);
            // 有効な防衛座標がある場合、攻撃の軌道になっているタイルと重複している分をカウントする
            if (defenceTiles.Count > 0)
            {
                foreach (Tile defenceTile in defenceTiles)
                {
                    if (trajectoryTiles.Contains(defenceTile)) overlapCount++;
                } 
            }

            Debug.Log($"有効な防衛タイル数: {overlapCount}");

            // 重複しているタイルがなければ迎撃できないため、次の防衛ユニットの処理へ
            if (overlapCount < 1) continue;

            // ====================================================
            // 防衛判定
            // ====================================================
            // ターゲットとHerringユニットのx座標の差（真正面からずれるほど、命中減衰率が増加する要因になる）
            float distanceX = Mathf.Abs(tgtPos.x - herringTile.Stats.GridPos.x);
            // 攻撃が迎撃を回避する確率
            float attackerEvasionRate = Random.value;
            // 防衛ユニットのy座標の防衛距離
            int verticalRange = herringUnit.Stats.VerticalRange;
            // 防衛ユニットプロファイル
            DefenceProfile defenceProfile = herringUnit.Stats.defenceProfile;
            // 防衛数分の判定処理を実行
            for (int i = 0; i < overlapCount; i++)
            {
                // 命中減衰率を踏まえた最終的な迎撃成功率を計算
                float xOffsetPenaltyRate = defenceProfile.accuracyDecay * distanceX;
                float yOffsetPenaltyRate = (verticalRange - i) * defenceProfile.accuracyDecay;
                float defencerHitRate = defenceProfile.accuracy -  xOffsetPenaltyRate - yOffsetPenaltyRate;
                // 防衛判定結果
                bool result = attackerEvasionRate < defencerHitRate;

                Debug.Log($"{i + 1}回目 => 攻撃側: {attackerEvasionRate} 防衛側: {defencerHitRate} {(result ? "成功" : "失敗")}");

                // 防衛成功判定を受け取った場合は、迎撃できたポジションを返す
                if (result)
                {
                    interceptingUnitTile = herringTile;
                    interceptGridPos = new Vector2Int(tgtPos.x, herringTile.Stats.GridPos.y + verticalRange - i);
                    return true;
                }
            }
        }

        interceptingUnitTile = null;
        interceptGridPos = Vector2Int.zero;
        return false;
    }
}


