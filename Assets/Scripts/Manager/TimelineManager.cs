using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TileOwner = TileStats.TileOwner;

public class TimelineManager : MonoBehaviour, IInitializable
{
    public static TimelineManager Instance { get; private set; }

    // コマンド情報をまとめたクラス
    [System.Serializable]
    public class TimelineCommand
    {
        public TileOwner Owner;
        public string UnitName;
        public Tile AttackerTile;
        public Tile TargetTile;  // 攻撃対象の中心タイル
        public List<Tile> AffectedTiles; 
        public float Damage;        // ダメージ量
        public float Time; // 経過時間 + 適用必要時間

        public TimelineCommand(
            TileOwner owner,
            string unitName,
            Tile attackerTile,
            Tile targetTile,
            List<Tile> affectedTiles,
            float damage,
            float time
        ){
            Owner = owner;
            UnitName = unitName;
            AttackerTile = attackerTile;
            TargetTile = targetTile;
            AffectedTiles = affectedTiles;
            Damage = damage;
            Time = time;
        }
    }

    [SerializeField, Tooltip("攻撃タイムライン")]
    private List<TimelineCommand> _timeline = new List<TimelineCommand>();
    public int TimelineCount => _timeline.Count;

    [Header("Refs")]
    private GameManager _gameManager;
    private MapManager _mapManager;
    private TileManager _tileManager;
    private AttackManager _attackManager;
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
        _attackManager = AttackManager.Instance;
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
            Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++");
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
    private async UniTask ExecuteCommandAsync(TimelineCommand command)
    {
        // TODO: コマンド内容に応じて条件分岐させたい
        switch (command.AttackerTile.Unit.Stats.profile.unitType)
        {
            case UnitType.Squid:
                // 迎撃プロセスの実行
                await _attackManager.ProcessInkInterceptAttempt(command);
                break;
        }
    }

    /// <summary>
    /// コマンドを作成する（プレイヤー用）
    /// </summary>
    public TimelineCommand CreatePlayerCommand()
    {
        if (_tileManager.selectedTile.Unit == null ||
            _tileManager.selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new System.InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
        }

        UnitProfile profile = attackerUnit.Stats.profile;
        AttackProfile attackProfile = attackerUnit.Stats.attackProfile;

        return new TimelineCommand(
            TileOwner.Player,
            profile.unitName,
            _tileManager.selectedTile,
            _tileManager.targetTile,
            _tileManager.targetTiles,
            attackProfile.power,
            attackProfile.delay
        );
    }

    /// <summary>
    /// コマンドを作成する（エネミー用）
    /// </summary>
    public TimelineCommand CreateEnemyCommand()
    {
        Tile selectedTile = _mapManager.enemyMapData[0, 0];
        Tile targetTile = _mapManager.playerMapData[4, 4];

        if (selectedTile.Unit == null ||
            selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new System.InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
        }

        List<Tile> affectedTiles = new List<Tile>();
        List<Vector2Int> affectedPositions = attackerUnit.Controller.GetTargetTilePositions(targetTile.Stats.GridPos);
        foreach (Vector2Int pos in affectedPositions)
        {
            Tile tile = _mapManager.GetPlayerTile(pos);

            if (tile == null) continue;
            // 配列（リスト）に保存
            affectedTiles.Add(tile);
        }

        return new TimelineCommand(
            TileOwner.Enemy,
            attackerUnit.Stats.profile.unitName,
            selectedTile,
            targetTile,
            affectedTiles,
            attackerUnit.Stats.attackProfile.power,
            attackerUnit.Stats.attackProfile.delay
        );
    }

    /// <summary>
    /// コマンドを予約する
    /// </summary>
    public void RegisterCommand(TimelineCommand command)
    {
        // 攻撃内容をキューに追加
        _timeline.Add(command);
        // 時間の小さい順にする
        _timeline.Sort((a, b) => b.Time.CompareTo(a.Time));
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_timeline);
        
        Debug.Log($"攻撃予約完了！");
    }

    // public void RegisterCommand()
    // {
    //     if (_tileManager.selectedTile.Unit is not AttackerUnitBase attackerUnit)
    //     {
    //         throw new System.InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
    //     }

    //     UnitProfile profile = attackerUnit.Stats.profile;
    //     AttackProfile attackProfile = attackerUnit.Stats.attackProfile;
    //     // 攻撃内容を作成してキューに追加
    //     TimelineCommand newAttack = new TimelineCommand(
    //         _tileManager.selectedTile.Stats.owner,
    //         profile.unitName,
    //         _tileManager.selectedTile,
    //         _tileManager.targetTile,
    //         new List<Tile>(_tileManager.targetTiles),
    //         attackProfile.power,
    //         attackProfile.delay
    //     );
    //     _timeline.Add(newAttack);
    //     _timeline.Sort((a, b) => b.time.CompareTo(a.time));
    //     _timelinePresenter.UpdateTimeline(_timeline);
        
    //     Debug.Log($"攻撃予約完了！");
    // }
}
