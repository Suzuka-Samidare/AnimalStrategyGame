using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TimelineCommand = TimelineManager.TimelineCommand;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }

    [Serializable]
    public readonly struct InterceptionDetail
    {
        /// <summary> 迎撃を行ったユニットが存在するタイル </summary>
        public Tile InterceptedUnitTile { get; }
        /// <summary> 迎撃が発生した座標（着弾・弾道位置など） </summary>
        public Tile InterceptionTile { get; }
        /// <summary> 迎撃によって発生したダメージ量 </summary>
        public float Damage { get; }

        public InterceptionDetail(Tile interceptedUnitTile, Tile interceptionTile, float damage)
        {
            InterceptedUnitTile = interceptedUnitTile;
            InterceptionTile = interceptionTile;
            Damage = damage;
        }

        /// <summary> 迎撃失敗時のデフォルト値を取得 </summary>
        public static InterceptionDetail Fail (Tile interceptedUnitTile)
        {
            return new InterceptionDetail(interceptedUnitTile, null, 0);
        }
    }

    [Serializable]
    public readonly struct InterceptionResult
    {
        public TimelineCommand Command { get; }
        /// <summary> 迎撃が成功したかどうか </summary>
        public bool IsSuccess { get; }
        /// <summary> 迎撃の詳細結果のリスト </summary>
        public IReadOnlyList<InterceptionDetail> Details { get; }
        /// <summary> 敵のHPが0になった（撃破された）迎撃のインデックス。撃破されなかった場合は -1 </summary>
        public int FatalIndex { get; }

        public InterceptionResult(TimelineCommand command, bool isSuccess, IReadOnlyList<InterceptionDetail> details, int fatalIndex)
        {
            Command = command;
            IsSuccess = isSuccess;
            Details = details;
            FatalIndex = fatalIndex;
        }

        /// <summary> 敵のHPが0になった（撃破された）迎撃詳細（読み取り用） </summary>
        public InterceptionDetail? FatalDetail => FatalIndex >= 0 && FatalIndex < Details.Count
            ? Details[FatalIndex]
            : null;
    }

    [Header("Refs")]
    [SerializeField] private CombatPerformanceDirector _combatPerformanceDirector;
    private MapManager _mapManager;
    private ParticleManager _particleManager;
    [SerializeField] private DirectCombatManager _directCombatManager;

    [Header("Debug")]
    [SerializeField] private bool _isSuccess;
    [SerializeField] private int _detailCount;
    [SerializeField] private int _fatalIndex;

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

    public async UniTask ExecuteMultipleCommandsAsync(List<TimelineCommand> commands)
    {
        // 先頭にあるコマンド
        TimelineCommand firstCommand = commands[0];
        // 着弾までに通過する予定のタイル群
        List<Tile> trajectoryTiles = _mapManager.GetTrajectoryTiles(firstCommand.TargetTile);
        // 迎撃結果の取得（先頭のコマンドに登録されているユニットで計算する）
        InterceptionResult interceptionResult = GetHerringsDefenceResult(firstCommand, trajectoryTiles);

        // ===DEBUG=============================
        Debug.Log($"IsSuccess: {interceptionResult.IsSuccess}");
        Debug.Log($"Details.Count: {interceptionResult.Details.Count}");
        Debug.Log($"FatalIndex: {interceptionResult.FatalIndex}");
        _isSuccess = interceptionResult.IsSuccess;
        _detailCount = interceptionResult.Details.Count;
        _fatalIndex = interceptionResult.FatalIndex;
        // ===DEBUG=============================

        // ユニットの視認状態の更新
        UpdateUnitVisibility(interceptionResult, trajectoryTiles);
        // コマンドの攻撃元ユニットに応じて処理を実行
        switch (firstCommand.AttackerUnit.Stats.profile.unitType)
        {
            case UnitType.Squid:
                // =====================================================
                // ダメージ反映及び演出
                // 防錆成功 => 攻撃が迎撃される演出のみ
                // 防衛失敗 => 攻撃ヒット及びユニットの気絶演出
                // =====================================================
                Debug.Log($"isSuccessDefence: {interceptionResult.IsSuccess}");
                // 迎撃結果に応じて処理を分岐
                if (interceptionResult.IsSuccess)
                {
                    var fatalDetail = interceptionResult.Details[interceptionResult.FatalIndex];
                    // インクが迎撃される演出
                    await _combatPerformanceDirector.AttackInkFailed(
                        firstCommand,
                        fatalDetail.InterceptedUnitTile.Stats.GlobalPos,
                        fatalDetail.InterceptionTile.Stats.GlobalPos);
                }
                else
                {
                    // 内部的なダメージの反映（見た目に反映されないAPI通信に近い更新）
                    ApplyAreaDamage(firstCommand);
                    // インク攻撃が着弾する演出
                    await _combatPerformanceDirector.AttackInkSuccess(firstCommand);
                    // TODO: AttackHitEffectsをAttackInkSuccessに統合する
                    await AttackHitEffects(firstCommand);
                    // TODO: CombatPerformanceDirectorに移行する
                    await FaintEffects(firstCommand);
                }
                break;
            case UnitType.Colobus:
                // 影響タイル群からColobusが下りられる地点をランダムにコマンドの数だけ選出する
                List<Tile> emptyTiles = firstCommand.AffectedTiles.FindAll(tile => tile.IsExistUnit == false);
                Tile[] arrivalTiles = TileManager.Instance.GetRandomEnptyTiles(emptyTiles, commands.Count);
                // 迎撃結果に応じて処理を分岐
                if (interceptionResult.IsSuccess)
                {
                }
                else
                {
                    foreach (var tile in commands[0].AffectedTiles)
                    {  
                        if (tile.IsExistUnit) tile.Unit.SetVisible(true);
                    }
                    // ユニットの移動演出
                    await _combatPerformanceDirector.AirdropColobus(commands, arrivalTiles);
                    await _directCombatManager.StartCombatAsync(commands);
                    await _combatPerformanceDirector.ResetUnitPosition(commands);
                }
                break;
        }
        // 攻撃予約済みフラグを解除する
        foreach (var cmd in commands)
        {
            cmd.AttackerUnit.DisableAttackSchedule();
        }
    }

    /// <summary>
    /// 影響タイル（ユニット）へのダメージ反映
    /// </summary>
    public void ApplyAreaDamage(TimelineCommand command)
    {
        foreach (Tile tile in command.AffectedTiles)
        {
            if (tile.IsExistUnit)
            {
                tile.Unit.Stats.ApplyDamage(command.Damage);
                // UniTask damageTask = tile.UnitBase.Controller.ApplyDamage(command.Damage, tile);
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
    /// 共用迎撃プロセス
    /// </summary>
    // public void ResolveInterception(TimelineCommand command)
    // {
    //     // 着弾までに通過する予定のタイル群
    //     List<Tile> trajectoryTiles = _mapManager.GetTrajectoryTiles(command.TargetTile);
    //     // 迎撃結果の取得
    //     InterceptionResult interceptionResult = GetHerringsDefenceResult(command, trajectoryTiles);
    //     // ユニットの視認状態の更新
    //     UpdateUnitVisibility(interceptionResult, trajectoryTiles);

    //     // // 完全に迎撃されたタイル
    //     // // Tile interceptedTile = command.Owner == Owner.Player
    //     // //     ? _mapManager.GetEnemyTile(interceptGridPos, true)
    //     // //     : _mapManager.GetPlayerTile(interceptGridPos, true);
    //     // // Vector3 interceptedPos = interceptedTile.Stats.GlobalPos;
    //     // // 攻撃が最終的に届いた最も奥のタイル
    //     // Tile FarthestAttackReachedTile = null;
    //     // foreach (var detail in interceptionResult.Details)
    //     // {
    //     //     if (FarthestAttackReachedTile == null
    //     //         || detail.InterceptionTile.Stats.GridPos.y < FarthestAttackReachedTile.Stats.GridPos.y)
    //     //     {
    //     //         FarthestAttackReachedTile = detail.InterceptionTile;
    //     //     }
    //     // }

    //     // // =====================================================
    //     // // ユニットの視認状態の更新
    //     // // =====================================================
    //     // // 着弾までに実際に通過したタイル群（最前線で迎撃されてもメタタイルも含めるので、必ず1つ以上ある）
    //     // List<Tile> actualTrajectoryTiles = trajectoryTiles.FindAll(tile => tile.Stats.GridPos.y >= FarthestAttackReachedTile.Stats.GridPos.y);
    //     // // 実際に通過したタイルから最奥タイル（グリッドのy座標が最小のタイル）を取得する
    //     // Tile actualTrajectoryEndTile = actualTrajectoryTiles[0];
    //     // if (actualTrajectoryTiles.Count > 0)
    //     // {
    //     //     foreach (Tile tile in actualTrajectoryTiles)
    //     //     {
    //     //         if (tile.Stats.GridPos.y < actualTrajectoryEndTile.Stats.GridPos.y)
    //     //         {
    //     //             actualTrajectoryEndTile = tile;
    //     //         }
    //     //     }
    //     // }
    //     // // 最奥タイルのさらに2マス分のグリッド座標の生成
    //     // List<Vector2Int> scoutedGridPos = new List<Vector2Int> {
    //     //     actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -1),
    //     //     actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -2)
    //     // };
    //     // // 生成した座標を元に、偵察で視認が出来たタイルとして取得する
    //     // List<Tile> scoutedTiles = command.Owner == Owner.Player
    //     //     ? _mapManager.GetEnemyTiles(scoutedGridPos, true)
    //     //     : _mapManager.GetPlayerTiles(scoutedGridPos, true);
    //     // // 着弾までに実際に通過したタイルを偵察で視認出来たタイルとして追加する
    //     // scoutedTiles.AddRange(actualTrajectoryTiles);
    //     // // 偵察で視認出来たタイルと、これを基点にした左右の隣接タイルも最終的な視認可能タイルとして登録する
    //     // List<Tile> visibleTiles = new List<Tile>(scoutedTiles);
    //     // visibleTiles.AddRange(_mapManager.GetFlankingTiles(command.TargetTile.Stats.owner, scoutedTiles));
    //     // // リストにあるタイルを視認可能状態に更新する
    //     // foreach (Tile tile in visibleTiles)
    //     // {
    //     //     if (tile.IsExistUnit) tile.Unit.SetVisible(true);
    //     // }
    // }

    /// <summary>
    /// Squid攻撃反映プロセス
    /// </summary>
    public async UniTask ApplySquidDamage(TimelineCommand command, bool isInterceptionSuccessful, Tile interceptingUnitTile, Vector3 interceptedPos)
    {
        // =====================================================
        // ダメージ反映及び演出
        // 防錆成功 => 攻撃が迎撃される演出のみ
        // 防衛失敗 => 攻撃ヒット及びユニットの気絶演出
        // =====================================================
        Debug.Log($"isInterceptionSuccessful: {isInterceptionSuccessful}");
        Debug.Log($"interceptingUnitTile: {interceptingUnitTile}");
        Debug.Log($"interceptedPos: {interceptedPos}");
        // 迎撃結果に応じて処理を分岐
        if (isInterceptionSuccessful)
        {
            // インクが迎撃される演出
            await _combatPerformanceDirector.AttackInkFailed(command, interceptingUnitTile.Stats.GlobalPos, interceptedPos);
        }
        else
        {
            // 内部的なダメージの反映（見た目に反映されないAPI通信に近い更新）
            ApplyAreaDamage(command);
            // インク攻撃が着弾する演出
            await _combatPerformanceDirector.AttackInkSuccess(command);
            // TODO: AttackHitEffectsをAttackInkSuccessに統合する
            await AttackHitEffects(command);
            // TODO: CombatPerformanceDirectorに移行する
            await FaintEffects(command);
        }
    }

    /// <summary>
    /// インク迎撃プロセス
    /// </summary>
    // public async UniTask ProcessInkInterceptAttempt(TimelineCommand command)
    // {
    //     // =====================================================
    //     // 攻撃結果の取得
    //     // =====================================================
    //     // 防衛が成功したか
    //     bool isSuccessDefence;
    //     // 迎撃したユニットのタイル（いなければnull）
    //     Tile interceptingUnitTile;
    //     // 迎撃された座標（グリッド座標）
    //     Vector2Int interceptGridPos;
    //     // 着弾までに通過する予定のタイル群
    //     List<Tile> trajectoryTiles = _mapManager.GetTrajectoryTiles(command.TargetTile);
    //     // 迎撃結果の取得
    //     isSuccessDefence = GetHerringsDefenceResult(command, trajectoryTiles, out interceptingUnitTile, out interceptGridPos);
    //     // 迎撃されたタイル
    //     Tile interceptedTile = command.Owner == Owner.Player
    //         ? _mapManager.GetEnemyTile(interceptGridPos, true)
    //         : _mapManager.GetPlayerTile(interceptGridPos, true);
    //     Vector3 interceptedPos = interceptedTile.Stats.GlobalPos;

    //     // =====================================================
    //     // ユニットの視認状態の更新
    //     // =====================================================
    //     // 着弾までに実際に通過したタイル群（最前線で迎撃されてもメタタイルも含めるので、必ず1つ以上ある）
    //     List<Tile> actualTrajectoryTiles = trajectoryTiles.FindAll(tile => tile.Stats.GridPos.y >= interceptedTile.Stats.GridPos.y);
    //     // 実際に通過したタイルから最奥タイル（グリッドのy座標が最小のタイル）を取得する
    //     Tile actualTrajectoryEndTile = actualTrajectoryTiles[0];
    //     if (actualTrajectoryTiles.Count > 0)
    //     {
    //         foreach (Tile tile in actualTrajectoryTiles)
    //         {
    //             if (tile.Stats.GridPos.y < actualTrajectoryEndTile.Stats.GridPos.y)
    //             {
    //                 actualTrajectoryEndTile = tile;
    //             }
    //         }
    //     }
    //     // 最奥タイルのさらに2マス分のグリッド座標の生成
    //     List<Vector2Int> scoutedGridPos = new List<Vector2Int> {
    //         actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -1),
    //         actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -2)
    //     };
    //     // 生成した座標を元に、偵察で視認が出来たタイルとして取得する
    //     List<Tile> scoutedTiles = command.Owner == Owner.Player
    //         ? _mapManager.GetEnemyTiles(scoutedGridPos, true)
    //         : _mapManager.GetPlayerTiles(scoutedGridPos, true);
    //     // 着弾までに実際に通過したタイルを偵察で視認出来たタイルとして追加する
    //     scoutedTiles.AddRange(actualTrajectoryTiles);
    //     // 偵察で視認出来たタイルと、これを基点にした左右の隣接タイルも最終的な視認可能タイルとして登録する
    //     List<Tile> visibleTiles = new List<Tile>(scoutedTiles);
    //     visibleTiles.AddRange(_mapManager.GetFlankingTiles(command.TargetTile.Stats.owner, scoutedTiles));
    //     // リストにあるタイルを視認可能状態に更新する
    //     foreach (Tile tile in visibleTiles)
    //     {
    //         if (tile.IsExistUnit) tile.Unit.SetVisible(true);
    //     }


    //     // =====================================================
    //     // ダメージ反映及び演出
    //     // 防錆成功 => 攻撃が迎撃される演出のみ
    //     // 防衛失敗 => 攻撃ヒット及びユニットの気絶演出
    //     // =====================================================
    //     Debug.Log($"isSuccessDefence: {isSuccessDefence}");
    //     // 迎撃結果に応じて処理を分岐
    //     if (isSuccessDefence)
    //     {
    //         // インクが迎撃される演出
    //         await _combatPerformanceDirector.AttackInkFailed(command, interceptingUnitTile.Stats.GlobalPos, interceptedPos);
    //     }
    //     else
    //     {
    //         // 内部的なダメージの反映（見た目に反映されないAPI通信に近い更新）
    //         ApplyDamage(command);
    //         // インク攻撃が着弾する演出
    //         await _combatPerformanceDirector.AttackInkSuccess(command);
    //         // TODO: AttackHitEffectsをAttackInkSuccessに統合する
    //         await AttackHitEffects(command);
    //         // TODO: CombatPerformanceDirectorに移行する
    //         await FaintEffects(command);
    //     }
    // }

    /// <summary>
    /// Colobusユニットの攻撃処理まとめ
    /// </summary>
    // public async UniTask ApplyColobusDamage(TimelineCommand command)
    // {
    //     // ユニットを敵地に
    //     await _combatPerformanceDirector.AirdropColobus(command, true);

    //     Timer timeLeft = new Timer();
    // }

    private async UniTask AttackHitEffects(TimelineCommand command)
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

    private async UniTask FaintEffects(TimelineCommand command)
    {
        List<UniTask> animationTasks = new List<UniTask>();
        foreach (Tile tile in command.AffectedTiles)
        {
            // 気絶している場合は、アニメーション
            if (tile.Unit != null && tile.Unit.Stats.IsFaint) {
                UniTask faint = tile.OnFaintUnit();
                animationTasks.Add(faint);
            }
        }
        await UniTask.WhenAll(animationTasks.ToArray());
    }

    /// <summary>
    /// Herringユニット1体による迎撃詳細を返す
    /// </summary>
    private InterceptionDetail CalculateHerringIntercept(Tile herringTile, Vector2Int tgtGridPos, int overlapCount)
    {
        HerringUnit herringUnit = herringTile.Unit as HerringUnit;

        // ====================================================
        // 防衛判定
        // ====================================================
        // ターゲットとHerringユニットのx座標の差（真正面からずれるほど、命中減衰率が増加する要因になる）
        float distanceX = Mathf.Abs(tgtGridPos.x - herringTile.Stats.GridPos.x);
        // 攻撃が迎撃を回避する確率
        float attackerEvasionRate = UnityEngine.Random.value;
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

            // 防衛成功判定を受け取った場合は、迎撃できたポジションを添えた迎撃結果を返して処理を終える
            if (result)
            {
                // 迎撃されたタイル
                Vector2Int interceptGridPos = new Vector2Int(tgtGridPos.x, herringTile.Stats.GridPos.y + verticalRange - i);
                Tile interceptedTile = herringTile.Stats.owner == Owner.Player
                    ? _mapManager.GetPlayerTile(interceptGridPos, true)
                    : _mapManager.GetEnemyTile(interceptGridPos, true);
                
                return new InterceptionDetail(herringTile, interceptedTile, herringUnit.Stats.defenceProfile.power);
            }
        }

        // 防衛に失敗した場合は、迎撃ポジションがnullになっている結果を返す
        return InterceptionDetail.Fail(herringTile);
    }

    /// <summary>
    /// 迎撃可能なHerringユニットによる迎撃成否結果を返す
    /// </summary>
    private InterceptionResult GetHerringsDefenceResult(TimelineCommand command, List<Tile> trajectoryTiles)
    {
        // 迎撃結果のリスト
        var interceptionDetails = new List<InterceptionDetail>();
        // ターゲットの座標
        Vector2Int tgtGridPos = command.TargetTile.Stats.GridPos;
        // 相手陣地のHerringユニットがいるタイルの取得
        List<Tile> herringTiles = command.Owner == Owner.Player
            ? _mapManager.GetEnemyMapHerringTiles()
            : _mapManager.GetPlayerMapHerringTiles();
        // 総ダメージ
        float totalDamage = 0;
        // 迎撃詳細のインデックス
        int fatalIndex = -1;
        // 発射物の最大HP
        float projectileMaxHp = command.AttackerUnit.Stats.projectileProfile.maxHp;
        // y座標値が高い順に並べ替え
        herringTiles.Sort((a, b) => b.Stats.GridPos.y.CompareTo(a.Stats.GridPos.y));
        // 各Herringユニットごとに防衛処理
        for (int i = 0; i < herringTiles.Count; i++)
        {
            Tile herringTile = herringTiles[i];
            HerringUnit herringUnit = herringTile.Unit as HerringUnit;
            // Herringユニットの左右防衛幅
            int horizonRange = herringUnit.Stats.defenceProfile.range.max;
            // ターゲットのx座標がHerringユニットの防衛幅に入らない場合はスキップ
            if (tgtGridPos.x < herringTile.Stats.GridPos.x - horizonRange || tgtGridPos.x > herringTile.Stats.GridPos.x + horizonRange) continue;

            // ====================================================
            // マップ内の防衛座標の集計
            // ====================================================
            // Herringユニットの防衛有効範囲の2次元グリッド座標を取得
            List<Vector2Int> defencePositions = herringUnit.Controller.GetDefensiveRangePos(herringTile.Stats.GridPos);
            // 2次元グリッド座標を元に防衛有効範囲タイルを取得
            List<Tile> defenceTiles = herringTile.Stats.owner == Owner.Player
                ? _mapManager.GetPlayerTiles(defencePositions, true)
                : _mapManager.GetEnemyTiles(defencePositions, true);
            // 攻撃の軌道になっているタイルと重複している分をカウントする
            int overlapCount = GetOverlapTileCount(defenceTiles, trajectoryTiles); 
            Debug.Log($"有効な防衛タイル数: {overlapCount}");
            // 重複しているタイルがなければ迎撃できないため、次の防衛ユニットの処理へ
            if (overlapCount < 1) continue;

            // 迎撃処理結果の取得
            InterceptionDetail detail = CalculateHerringIntercept(herringTile, tgtGridPos, overlapCount);
            // 迎撃処理の結果をリストに格納
            interceptionDetails.Add(detail);
            // 総合ダメージを加算
            totalDamage += detail.Damage;
            // 総ダメージが発射物の最大HPを上回った迎撃詳細のインデックスを保持
            if (totalDamage >= projectileMaxHp) fatalIndex = interceptionDetails.Count - 1;
        }

        // 飛来物の最大HPを総ダメージを上回っているかの真偽値を追加して、迎撃結果を返す
        return new InterceptionResult(command, totalDamage >= projectileMaxHp, interceptionDetails, fatalIndex);
    }

    /// <summary>
    /// ユニットの視認状態の更新
    /// </summary>
    private void UpdateUnitVisibility(InterceptionResult interceptionResult, List<Tile> trajectoryTiles)
    {

        // 攻撃が最終的に届いた一番奥のタイル（デフォルトはターゲットタイル）
        Tile FarthestAttackReachedTile = interceptionResult.FatalDetail?.InterceptionTile ?? interceptionResult.Command.TargetTile;
        // foreach (var detail in interceptionResult.Details)
        // {
        //     if (detail.InterceptionTile.Stats.GridPos.y < FarthestAttackReachedTile.Stats.GridPos.y)
        //     {
        //         FarthestAttackReachedTile = detail.InterceptionTile;
        //     }
        // }
        // 着弾までに実際に通過したタイル群（最前線で迎撃されてもメタタイルも含めるので、必ず1つ以上ある）
        List<Tile> actualTrajectoryTiles = trajectoryTiles.FindAll(tile => tile.Stats.GridPos.y >= FarthestAttackReachedTile.Stats.GridPos.y);
        // // 実際に通過したタイルから最奥タイル（グリッドのy座標が最小のタイル）を取得する
        // Tile actualTrajectoryEndTile = actualTrajectoryTiles[0];
        // if (actualTrajectoryTiles.Count > 0)
        // {
        //     foreach (Tile tile in actualTrajectoryTiles)
        //     {
        //         if (tile.Stats.GridPos.y < actualTrajectoryEndTile.Stats.GridPos.y)
        //         {
        //             actualTrajectoryEndTile = tile;
        //         }
        //     }
        // }
        // 最奥タイルのさらに2マス分のグリッド座標の生成
        // List<Vector2Int> scoutedGridPos = new List<Vector2Int> {
        //     actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -1),
        //     actualTrajectoryEndTile.Stats.GridPos + new Vector2Int(0, -2)
        // };
        // 最奥タイルのさらに2マス分のグリッド座標の生成
        List<Vector2Int> scoutedGridPos = new List<Vector2Int> {
            FarthestAttackReachedTile.Stats.GridPos + new Vector2Int(0, -1),
            FarthestAttackReachedTile.Stats.GridPos + new Vector2Int(0, -2)
        };
        // 生成した座標を元に、偵察で視認が出来たタイルとして取得する
        List<Tile> scoutedTiles = interceptionResult.Command.Owner == Owner.Player
            ? _mapManager.GetEnemyTiles(scoutedGridPos, true)
            : _mapManager.GetPlayerTiles(scoutedGridPos, true);
        // 着弾までに実際に通過したタイルを偵察で視認出来たタイルとして追加する
        scoutedTiles.AddRange(actualTrajectoryTiles);
        // 偵察で視認出来たタイルと、これを基点にした左右の隣接タイルも最終的な視認可能タイルとして登録する
        List<Tile> visibleTiles = new List<Tile>(scoutedTiles);
        visibleTiles.AddRange(_mapManager.GetFlankingTiles(interceptionResult.Command.TargetTile.Stats.owner, scoutedTiles));
        // リストにあるタイルを視認可能状態に更新する
        foreach (Tile tile in visibleTiles)
        {
            if (tile.IsExistUnit) tile.Unit.SetVisible(true);
        }
    }

    /// <summary>
    /// 2つのタイルリストの重複数を高速に集計する（GC Alloc ゼロ）
    /// </summary>
    private int GetOverlapTileCount(List<Tile> defenceTiles, List<Tile> trajectoryTiles)
    {
        int overlapCount = 0;
        int defCount = defenceTiles.Count;
        int trajCount = trajectoryTiles.Count;

        for (int d = 0; d < defCount; d++)
        {
            Tile defTile = defenceTiles[d];
            for (int t = 0; t < trajCount; t++)
            {
                if (defTile == trajectoryTiles[t])
                {
                    overlapCount++;
                    break; // 1つの防衛タイルにつき重なりは1回のみカウント
                }
            }
        }

        return overlapCount;
    }

    // private bool GetHerringsDefenceResult(
    //     TimelineCommand command,
    //     List<Tile> trajectoryTiles,
    //     out Tile interceptingUnitTile,
    //     out Vector2Int interceptGridPos)
    // {
    //     // ターゲットの座標
    //     Vector2Int tgtPos = command.TargetTile.Stats.GridPos;
    //     // 攻撃が着弾するまでに通過するタイル
    //     // List<Tile> trajectoryTiles = _mapManager.GetTrajectoryTiles(command.TargetTile);
    //     // Herringユニットがいるタイル
    //     List<Tile> herringTiles = command.Owner == Owner.Player
    //         ? _mapManager.GetEnemyMapHerringTiles()
    //         : _mapManager.GetPlayerMapHerringTiles();
    //     // y座標値が高い順に並べ替え
    //     herringTiles.Sort((a, b) => b.Stats.GridPos.y.CompareTo(a.Stats.GridPos.y));

    //     // Debug.Log($"Herringユニットの総数: {herringTiles.Count}");

    //     // 各Herringユニットごとに防衛処理
    //     foreach (var herringTile in herringTiles)
    //     {
    //         HerringUnit herringUnit = herringTile.Unit as HerringUnit;
    //         // Herringユニットの左右防衛幅
    //         int horizonRange = herringUnit.Stats.defenceProfile.range.max;
    //         // ターゲットのx座標がHerringユニットの防衛幅に入らない場合はスキップ
    //         if (tgtPos.x < herringTile.Stats.GridPos.x - horizonRange || tgtPos.x > herringTile.Stats.GridPos.x + horizonRange) continue;

    //         // Herringユニットの防衛座標リストを取得
    //         List<Vector2Int> defencePositions = herringUnit.Controller.GetDefensiveRangePos(herringTile.Stats.GridPos);
    //         // 有効な防衛タイル数の集計
    //         int overlapCount = 0;

    //         // ====================================================
    //         // マップ内の防衛座標の集計
    //         // ====================================================
    //         // マップ内で有効な防衛座標リストを取得
    //         List<Tile> defenceTiles = herringTile.Stats.owner == Owner.Player
    //             ? _mapManager.GetPlayerTiles(defencePositions, true)
    //             : _mapManager.GetEnemyTiles(defencePositions, true);
    //         // 有効な防衛座標がある場合、攻撃の軌道になっているタイルと重複している分をカウントする
    //         if (defenceTiles.Count > 0)
    //         {
    //             foreach (Tile defenceTile in defenceTiles)
    //             {
    //                 if (trajectoryTiles.Contains(defenceTile)) overlapCount++;
    //             } 
    //         }

    //         Debug.Log($"有効な防衛タイル数: {overlapCount}");

    //         // 重複しているタイルがなければ迎撃できないため、次の防衛ユニットの処理へ
    //         if (overlapCount < 1) continue;

    //         // ====================================================
    //         // 防衛判定
    //         // ====================================================
    //         // ターゲットとHerringユニットのx座標の差（真正面からずれるほど、命中減衰率が増加する要因になる）
    //         float distanceX = Mathf.Abs(tgtPos.x - herringTile.Stats.GridPos.x);
    //         // 攻撃が迎撃を回避する確率
    //         float attackerEvasionRate = Random.value;
    //         // 防衛ユニットのy座標の防衛距離
    //         int verticalRange = herringUnit.Stats.VerticalRange;
    //         // 防衛ユニットプロファイル
    //         DefenceProfile defenceProfile = herringUnit.Stats.defenceProfile;
    //         // 防衛数分の判定処理を実行
    //         for (int i = 0; i < overlapCount; i++)
    //         {
    //             // 命中減衰率を踏まえた最終的な迎撃成功率を計算
    //             float xOffsetPenaltyRate = defenceProfile.accuracyDecay * distanceX;
    //             float yOffsetPenaltyRate = (verticalRange - i) * defenceProfile.accuracyDecay;
    //             float defencerHitRate = defenceProfile.accuracy -  xOffsetPenaltyRate - yOffsetPenaltyRate;
    //             // 防衛判定結果
    //             bool result = attackerEvasionRate < defencerHitRate;

    //             Debug.Log($"{i + 1}回目 => 攻撃側: {attackerEvasionRate} 防衛側: {defencerHitRate} {(result ? "成功" : "失敗")}");

    //             // 防衛成功判定を受け取った場合は、迎撃できたポジションを返す
    //             if (result)
    //             {
    //                 interceptingUnitTile = herringTile;
    //                 interceptGridPos = new Vector2Int(tgtPos.x, herringTile.Stats.GridPos.y + verticalRange - i);
    //                 return true;
    //             }
    //         }
    //     }

    //     interceptingUnitTile = null;
    //     interceptGridPos = Vector2Int.zero;
    //     return false;
    // }
}


