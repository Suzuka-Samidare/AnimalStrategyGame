using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TimelineManager : MonoBehaviour, IInitializable
{
    public static TimelineManager Instance { get; private set; }

    // コマンド情報をまとめたクラス
    [System.Serializable]
    public class TimelineCommand
    {
        public Owner Owner;
        public AttackerUnitBase AttackerUnit;
        public string UnitName;
        public Tile AttackerTile;
        public Tile TargetTile;  // 攻撃対象の中心タイル
        public List<Tile> AffectedTiles; 
        public float Damage;        // ダメージ量
        public float Time; // 経過時間 + 適用必要時間

        public TimelineCommand(
            Owner owner,
            AttackerUnitBase attackerUnit,
            Tile attackerTile,
            Tile targetTile,
            List<Tile> affectedTiles
        ){
            Owner = owner;
            AttackerUnit = attackerUnit;
            UnitName = attackerUnit.Stats.profile.unitName;
            AttackerTile = attackerTile;
            TargetTile = targetTile;
            AffectedTiles = affectedTiles;
            Damage = attackerUnit.Stats.attackProfile.power;
            Time = attackerUnit.Stats.attackProfile.delay;
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

        // タイムラインの中身を完全クリアにする（一応）
        _timeline.Clear();
    }

    /// <summary>
    /// コマンドの実行
    /// </summary>
    private async UniTask ExecuteCommandAsync(TimelineCommand command)
    {
        // TODO: コマンド内容に応じて条件分岐させたい
        switch (command.AttackerUnit.Stats.profile.unitType)
        {
            case UnitType.Squid:
                // 迎撃プロセスの実行
                await _attackManager.ProcessInkInterceptAttempt(command);
                // タイムラインのコマンド有効性チェック
                CheckCommandValidity();
                // 攻撃予約済みフラグを解除する
                command.AttackerUnit.DisableAttackSchedule();
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
            Owner.Player,
            attackerUnit,
            _tileManager.selectedTile,
            _tileManager.targetTile,
            _tileManager.targetTiles
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
            Owner.Enemy,
            attackerUnit,
            selectedTile,
            targetTile,
            affectedTiles
        );
    }

    /// <summary>
    /// コマンドを予約する
    /// </summary>
    public void RegisterCommand(TimelineCommand command)
    {
        // コマンド内容をキューに追加
        _timeline.Add(command);
        // 時間の小さい順にする
        _timeline.Sort((a, b) => b.Time.CompareTo(a.Time));
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_timeline);
    }

    /// <summary>
    /// コマンドを除外する
    /// </summary>
    private void RemoveCommand(int index)
    {
        // 指定コマンドをキューから除外
        _timeline.RemoveAt(index);
        // 時間の小さい順にする
        _timeline.Sort((a, b) => b.Time.CompareTo(a.Time));
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_timeline);
    }

    /// <summary>
    /// コマンドの有効性をチェックし、有効なコマンド以外を除外する。
    /// </summary>
    public void CheckCommandValidity()
    {
        for (int i = _timeline.Count - 1; i > 0; i--)
        {
            if (_timeline[i].AttackerUnit.Stats.IsFaint)
            {
                RemoveCommand(i);
            }
        }
    }
}
