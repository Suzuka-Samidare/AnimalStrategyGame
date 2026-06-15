using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileOwner = TileController.TileOwner;

public class AttackManager : MonoBehaviour, IInitializable
{
    public static AttackManager Instance { get; private set; }

    // 攻撃の情報をまとめたクラス
    [System.Serializable]
    public class AttackCommand
    {
        public TileOwner Owner;
        public string UnitName;
        public TileController Attacker;
        public TileController Target;  // 攻撃対象の中心タイル
        public List<TileController> AffectedTiles; 
        public float Damage;        // ダメージ量
        public float time; // 経過時間 + 適用必要時間

        public AttackCommand(
            TileOwner owner,
            string unitName,
            TileController attacker,
            TileController target,
            List<TileController> tiles,
            float damage,
            float delay
        ){
            Owner = owner;
            UnitName = unitName;
            Attacker = attacker;
            Target = target;
            AffectedTiles = tiles;
            Damage = damage;
            time = delay;
        }
    }

    [SerializeField, Tooltip("攻撃タイムライン")]
    private List<AttackCommand> _timeline = new List<AttackCommand>();
    public int TimelineCount => _timeline.Count;

    [Header("Refs")]
    private GameManager _gameManager;
    private MapManager _mapManager;
    private TileManager _tileManager;
    private TimelinePresenter _timelinePresenter;

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

    private void ResolveDependencies()
    {
        _gameManager = GameManager.Instance;
        _mapManager = MapManager.Instance;
        _tileManager = TileManager.Instance;
        _timelinePresenter = TimelinePresenter.Instance;
    }

    public async UniTask Initialize()
    {
        ResolveDependencies();
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// タイムラインのコマンド呼び出し
    /// </summary>
    public async UniTask ProcessTimeline()
    {
        while (_timeline.Count > 0)
        {
            // 先頭コマンドの実行
            await ExecuteCommandAsync(_timeline[0]);
            // コマンドをタイムラインから除外
            _timeline.RemoveAt(0);
            _timelinePresenter.UpdateTimeline(_timeline);

            // マップデータ処理完了待ち
            await UniTask.WaitUntil(() => _mapManager.isDirty == false);
            // 双方どちらかの本部ユニット数が0の場合ゲームオーバーに
            if (_mapManager.PlayerHqCount < 1 || _mapManager.EnemyHqCount < 1)
            {
                _gameManager.IsGameOver = true;
                break;
            }
        }

        // タイムラインの中身を完全クリアにする（おまじない）
        _timeline.Clear();
    }

    /// <summary>
    /// コマンドの実行
    /// </summary>
    private async Task ExecuteCommandAsync(AttackCommand command)
    {
        // 防衛が成功したか
        bool isSuccessDefence;
        // 迎撃完了座標
        Vector2Int interceptedPos;
        // 防衛処理
        if (command.Owner == TileOwner.Player)
        {
            isSuccessDefence = GetEnemyDefenceResult(command, out interceptedPos);
            Debug.Log($"interceptedPos: {interceptedPos}");
        }
        else
        {
            // isSuccessDefence = GetPlayerDefenceResult();
            isSuccessDefence = false;
        }


        if (isSuccessDefence == true) return;

        Debug.Log("ダメージ反映に入ります");

        // 演出を管理するタスクのリストを用意（後で全部終わったかチェックするため）
        await ApplyDamage(command);

        // ===================================================
        // 見た目の演出
        // ===================================================
        // 攻撃対象へカメラ移動
        CameraMovement.Instance.SetDestination(new Vector3(command.Target.GlobalPos.x, 1, command.Target.GlobalPos.z));

        var squidController = command.Attacker.unitObject.GetComponent<SquidController>();
        if (squidController != null)
        {
            await squidController.LerpLaunch(command.Target.GlobalPos);
        }

        await AttackDirection(command);
        await FaintDirection(command);
    }

    private async UniTask ApplyDamage(AttackCommand command)
    {
        List<UniTask> applyTask = new List<UniTask>();
        foreach (TileController tile in command.AffectedTiles)
        {
            if (tile.isExistUnit)
            {
                UniTask damageTask = tile.UnitBase.Controller.ApplyDamageAsync(command.Damage, tile);
                // あとで一括待機するためにリストに入れておく
                applyTask.Add(damageTask);
            }
            else
            {
                // TODO: MISS表記を入れたい -> ユニットによる位置基準ではダメ
                // TODO: MISS表記しない場合 -> ACTION中に攻撃範囲を分かるようにしたい
                Debug.Log("ダメージを与えるユニットが、このタイルにはいません。");
            }
        }
        await UniTask.WhenAll(applyTask.ToArray());
    }

    // private async UniTask DefenceDirection(Vector2Int interceptedPos)
    // {
        
    // }

    private async UniTask AttackDirection(AttackCommand command)
    {
        List<UniTask> animationTasks = new List<UniTask>();
        foreach (TileController tile in command.AffectedTiles)
        {
            // 爆発のパーティクルを生成
            ParticlePoolManager.Instance.SpawnParticle(tile.transform.position + Vector3.up, Quaternion.identity);
            // ユニットがいない場合はここで処理終了
            if (!tile.isExistUnit) continue;
            // ダメージ表示
            UniTask damageText = FloatingTextPresenter.Instance.SpawnDamageAsync(tile.GlobalPos, command.Damage);
            animationTasks.Add(damageText);
        }
        await UniTask.WhenAll(animationTasks.ToArray());
    }


    private async UniTask FaintDirection(AttackCommand command)
    {
        List<UniTask> animationTasks = new List<UniTask>();
        foreach (TileController tile in command.AffectedTiles)
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
    private List<TileController> GetTrajectoryTiles(TileController target, TileController[,] mapData)
    {
        List<TileController> tiles = new List<TileController>();

        for (int y = target.gridPos.y; y < _mapManager.mapHeight; y++)
        {
            int x = target.gridPos.x;
            if (mapData[x, y] != null) tiles.Add(mapData[x, y]);
        }

        return tiles;
    }


    /// <summary>
    /// 敵エリアへの攻撃に対する防衛判定
    /// </summary>
    private bool GetEnemyDefenceResult(AttackCommand command, out Vector2Int interceptedPos)
    {
        // ターゲットの座標
        Vector2Int tgtPos = command.Target.gridPos;
        // 攻撃が着弾するまでに通過するタイルを取得
        List<TileController> trajectoryTiles = GetTrajectoryTiles(command.Target, _mapManager.enemyMapData);
        // Herringユニットがあるタイルを取得
        List<TileController> herringTiles = _mapManager.GetEnemyMapHerringTiles();
        // y座標値が高い順に並べ替え
        herringTiles.Sort((a, b) => b.gridPos.y.CompareTo(a.gridPos.y));

        Debug.Log($"Herringユニットの総数: {herringTiles.Count}");

        // 各Herringユニットごとに防衛処理
        foreach (var herringTile in herringTiles)
        {
            // Herringユニットの左右防衛幅
            int horizonRange = herringTile.UnitDefendable.Stats.profile.range.max;
            // ターゲットのx座標がHerringユニットの防衛幅に入らない場合はスキップ
            if (tgtPos.x < herringTile.gridPos.x - horizonRange || tgtPos.x > herringTile.gridPos.x + horizonRange) continue;

            // Herringユニットの防衛座標リストを取得
            List<Vector2Int> defencePositions = herringTile.UnitDefendable.Controller.GetDefensiveRangePos(herringTile.gridPos);
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
            List<TileController> defenceTiles = _mapManager.GetEnemyTiles(defencePositions);
            // 有効な防衛座標がある場合、攻撃の軌道になっているタイルと重複している分をカウントする
            if (defenceTiles.Count > 0)
            {
                foreach (TileController defenceTile in defenceTiles)
                {
                    // Debug.Log($"trajectoryTiles.Contains: {trajectoryTiles.Contains(defenceTile)}");
                    if (trajectoryTiles.Contains(defenceTile)) overlapCount++;
                } 
            }

            Debug.Log($"マップ内外を合わせた有効な防衛数の集計: {overlapCount}");

            // ターゲットとHerringユニットのx座標の差（命中減衰率に影響する）
            float distanceX = Mathf.Abs(tgtPos.x - herringTile.gridPos.x);
            // 今回の防衛ユニット攻撃の命中率
            float attackerEvasionRate = UnityEngine.Random.value;
            // 防衛ユニットのy座標の防衛距離
            int verticalRange = herringTile.UnitDefendable.Controller.VerticalRange;
            // 防衛判定結果を受け取る
            for (int i = 0; i < overlapCount; i++)
            {
                bool result = herringTile.UnitDefendable.Controller.IsIntercepted(attackerEvasionRate, i, distanceX);

                // 防衛成功判定を受け取った場合は、迎撃できたポジションを返す
                if (result)
                {
                    interceptedPos = new Vector2Int(tgtPos.x, herringTile.gridPos.y + verticalRange - i);
                    return true;
                }
            }
        }

        interceptedPos = Vector2Int.zero;
        return false;
    }

    /// <summary>
    /// コマンドを予約する（外部から呼ぶ）
    /// </summary>
    public void RegisterCommand()
    {
        UnitProfile profile = _tileManager.selectedTileController.UnitBase.Stats.profile;
        AttackProfile attackProfile = _tileManager.selectedTileController.UnitAttackable.Stats.profile;
        // 攻撃内容を作成してキューに追加
        AttackCommand newAttack = new AttackCommand(
            _tileManager.selectedTileController.owner,
            profile.unitName,
            _tileManager.selectedTileController,
            _tileManager.targetTile,
            new List<TileController>(_tileManager.targetTiles),
            attackProfile.power,
            attackProfile.delay
        );
        _timeline.Add(newAttack);
        _timeline.Sort((a, b) => b.time.CompareTo(a.time));
        _timelinePresenter.UpdateTimeline(_timeline);
        
        Debug.Log($"攻撃予約完了！");
    }
}


