using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TileOwner = TileStats.TileOwner;
using TimelineCommand = TimelineManager.TimelineCommand;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }

    [Header("Refs")]
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
    /// インク迎撃プロセス
    /// </summary>
    public async UniTask ProcessInkInterceptAttempt(TimelineCommand command)
    {
        // 防衛が成功したか
        bool isSuccessDefence;
        // 迎撃完了座標（グリッド）
        Vector2Int interceptedGridPos;
        // 迎撃完了座標（グローバル）
        Vector3 interceptedPos = Vector3.zero;
        // 防衛処理
        if (command.Owner == TileOwner.Player)
        {
            isSuccessDefence = GetEnemyDefenceResult(command, out interceptedGridPos);
            Tile interceptedTile = _mapManager.GetEnemyTile(interceptedGridPos, true);
            interceptedPos = interceptedTile.Stats.GlobalPos;
        }
        else
        {
            // isSuccessDefence = GetPlayerDefenceResult();
            isSuccessDefence = false;
        }

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
        if (command.Attacker.unitObject.TryGetComponent<SquidAttackVisualizer>(out var squidAttackVisualizer))
        {
            if (isSuccessDefence)
            {
                await squidAttackVisualizer.AttackInkFailed(command.Target.Stats.GlobalPos, interceptedPos, _mapManager.GetEnemyTile(new Vector2Int(5, 5), true).Stats.GlobalPos);
            }
            else
            {
                await squidAttackVisualizer.AttackInkSuccess(command.Target.Stats.GlobalPos);
            }
        }
        else
        {
            throw new System.Exception("SquidAttackVisualizerの参照に失敗しました。");
        }

        // 防衛に失敗している場合は、攻撃ヒット及びユニットの気絶演出
        if (!isSuccessDefence)
        {
            await AttackHitEffects(command);
            await FaintEffects(command);
        }
    }

    public void ApplyDamage(TimelineCommand command)
    {
        // List<UniTask> applyTask = new List<UniTask>();
        foreach (Tile tile in command.AffectedTiles)
        {
            if (tile.IsExistUnit)
            {
                tile.UnitBase.Controller.ApplyDamageAsync(command.Damage, tile);
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
        // await UniTask.WhenAll(applyTask.ToArray());
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
            if (tile.UnitBase != null && tile.UnitBase.Stats.IsFaint) {
                UniTask faint = tile.UnitBase.Controller.OnFaint(tile);
                animationTasks.Add(faint);
            }
        }
        await UniTask.WhenAll(animationTasks.ToArray());
    }

    // [ContextMenu("じっけん！")]
    // public void TestestFunc()
    // {
    //     Debug.Log("====== TestestFunc =================");
    //     GetEnemyDefenceResult(_timeline[0]);
    // }

    /// <summary>
    /// 攻撃物がターゲットに着弾するまでに通過するタイルの取得
    /// </summary>
    private List<Tile> GetTrajectoryTiles(Tile target, Tile[,] mapData)
    {
        List<Tile> tiles = new List<Tile>();

        for (int y = target.Stats.GridPos.y; y < _mapManager.mapHeight; y++)
        {
            int x = target.Stats.GridPos.x;
            if (mapData[x, y] != null) tiles.Add(mapData[x, y]);
        }

        return tiles;
    }


    /// <summary>
    /// 敵エリアへの攻撃に対する防衛判定
    /// </summary>
    public bool GetEnemyDefenceResult(TimelineCommand command, out Vector2Int interceptedPos)
    {
        // ターゲットの座標
        Vector2Int tgtPos = command.Target.Stats.GridPos;
        // 攻撃が着弾するまでに通過するタイルを取得
        List<Tile> trajectoryTiles = GetTrajectoryTiles(command.Target, _mapManager.enemyMapData);
        // Herringユニットがあるタイルを取得
        List<Tile> herringTiles = _mapManager.GetEnemyMapHerringTiles();
        // y座標値が高い順に並べ替え
        herringTiles.Sort((a, b) => b.Stats.GridPos.y.CompareTo(a.Stats.GridPos.y));

        Debug.Log($"Herringユニットの総数: {herringTiles.Count}");

        // 各Herringユニットごとに防衛処理
        foreach (var herringTile in herringTiles)
        {
            // Herringユニットの左右防衛幅
            int horizonRange = herringTile.UnitDefendable.Stats.profile.range.max;
            // ターゲットのx座標がHerringユニットの防衛幅に入らない場合はスキップ
            if (tgtPos.x < herringTile.Stats.GridPos.x - horizonRange || tgtPos.x > herringTile.Stats.GridPos.x + horizonRange) continue;

            // Herringユニットの防衛座標リストを取得
            List<Vector2Int> defencePositions = herringTile.UnitDefendable.Controller.GetDefensiveRangePos(herringTile.Stats.GridPos);
            // 有効な防衛タイル数の集計
            int overlapCount = 0;

            // ====================================================
            // マップ外の防衛座標の集計
            // ====================================================
            // yが10以上の座標を取得し、座標数をカウントに追加
            HashSet<int> uniqueYValues = new HashSet<int>();
            for (int i = 0; i < defencePositions.Count; i++)
            {
                int currentY = defencePositions[i].y;
                if (currentY >= 10) uniqueYValues.Add(currentY);
            }
            overlapCount += uniqueYValues.Count;

            Debug.Log($"マップ外の有効な防衛数の集計: {overlapCount}");

            // ====================================================
            // マップ内の防衛座標の集計
            // ====================================================
            // マップ内で有効な防衛座標リストを取得
            List<Tile> defenceTiles = _mapManager.GetEnemyTiles(defencePositions);
            // 有効な防衛座標がある場合、攻撃の軌道になっているタイルと重複している分をカウントする
            if (defenceTiles.Count > 0)
            {
                foreach (Tile defenceTile in defenceTiles)
                {
                    if (trajectoryTiles.Contains(defenceTile)) overlapCount++;
                } 
            }

            Debug.Log($"マップ内外を合わせた有効な防衛数の集計: {overlapCount}");

            // ターゲットとHerringユニットのx座標の差（命中減衰率に影響する）
            float distanceX = Mathf.Abs(tgtPos.x - herringTile.Stats.GridPos.x);
            // 今回の防衛ユニット攻撃の命中率
            float attackerEvasionRate = Random.value;
            // 防衛ユニットのy座標の防衛距離
            int verticalRange = herringTile.UnitDefendable.Controller.VerticalRange;
            // 防衛数分の判定処理を実行
            for (int i = 0; i < overlapCount; i++)
            {
                // 防衛判定結果を受け取る
                bool result = herringTile.UnitDefendable.Controller.IsIntercepted(attackerEvasionRate, i, distanceX);

                // 防衛成功判定を受け取った場合は、迎撃できたポジションを返す
                if (result)
                {
                    interceptedPos = new Vector2Int(tgtPos.x, herringTile.Stats.GridPos.y + verticalRange - i);
                    return true;
                }
            }
        }

        interceptedPos = Vector2Int.zero;
        return false;
    }
}


