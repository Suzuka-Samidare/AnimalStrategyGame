using System;
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
        public bool IsActionCompleted;

        public TimelineCommand(
            Owner owner,
            AttackerUnitBase attackerUnit,
            Tile attackerTile,
            Tile targetTile,
            List<Tile> affectedTiles,
            float time
        ){
            Owner = owner;
            AttackerUnit = attackerUnit;
            UnitName = attackerUnit.Stats.profile.unitName;
            AttackerTile = attackerTile;
            TargetTile = targetTile;
            AffectedTiles = affectedTiles;
            Damage = attackerUnit.Stats.attackProfile.power;
            Time = time;
            IsActionCompleted = false;
        }
    }

    [SerializeField, Tooltip("プレイヤー用タイムライン")]
    private List<TimelineCommand> _playerTimeline = new List<TimelineCommand>();
    [SerializeField, Tooltip("エネミー用タイムライン")]
    private List<TimelineCommand> _enemyTimeline = new List<TimelineCommand>();
    [SerializeField, Tooltip("マスタータイムライン")]
    private List<TimelineCommand> _timeline = new List<TimelineCommand>();
    [Tooltip("全体タイムラインのコマンド数")]
    public int TimelineCount => _timeline.Count;

    // Actions系
    public event Func<float> OnRequestPhaseElapsedTime;
    public event Action OnGameOverConditionMet;

    [Header("Refs")]
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
    // public async UniTask ProcessTimeline()
    // {
    //     _timelinePresenter.UpdateTimeline(_timeline);

    //     while (_timeline.Count > 0)
    //     {
    //         Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++");
    //         // 先頭コマンドの実行
    //         await ExecuteCommandAsync(_timeline[0]);
    //         // コマンドをタイムラインから除外
    //         _timeline.RemoveAt(0);
    //         _timelinePresenter.UpdateTimeline(_timeline);

            // // マップデータ処理完了待ち
            // await UniTask.WaitUntil(() => _mapManager.isDirty == false);
            // // 双方どちらかの本部ユニット数が0の場合は、ゲームオーバー状態であることを伝達する
            // if (_mapManager.PlayerHqCount < 1 || _mapManager.EnemyHqCount < 1)
            // {
            //     OnGameOverConditionMet.Invoke();
            //     break;
            // }
    //     }

    //     // 各タイムラインの中身を完全クリアにする
    //     _timeline.Clear();
    //     _playerTimeline.Clear();
    //     _enemyTimeline.Clear();
    // }

    public async UniTask ProcessTimeline()
    {
        // TODO: SortAndCombineTimelinesの最後に実行すべきか考える
        _timelinePresenter.UpdateTimeline(_timeline);
        // コマンドの器を用意する
        List<TimelineCommand> executeCommands = new List<TimelineCommand>();

        while (_timeline.Count > 0)
        {
            Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++");
            // 実行コマンド群のクリア処理
            executeCommands.Clear();
            // 先頭の1コマンドを追加する
            var firstCommand = _timeline[0];
            executeCommands.Add(firstCommand);
            // 先頭のコマンドがColobusだった場合
            if (firstCommand.AttackerUnit is ColobusUnit)
            {
                // 他のColobusコマンドを探して実行コマンドとして追加
                for (int i = 1; i < _timeline.Count; i++)
                {
                    if (_timeline[i].AttackerUnit is ColobusUnit) executeCommands.Add(_timeline[i]);
                }
            }
            // コマンド処理の実行
            await _attackManager.ExecuteMultipleCommandsAsync(executeCommands);
            // 実行済フラグを付けてタイムラインからコマンドを除外
            foreach (var cmd in executeCommands)
            {
                cmd.IsActionCompleted = true;
            }
            // 実行済コマンドをタイムライン上から除外
            RemoveSharedTimelineCommand(cmd => cmd.IsActionCompleted);
            // マップデータ処理完了待ち
            await UniTask.WaitUntil(() => _mapManager.isDirty == false);
            // 双方どちらかの本部ユニット数が0の場合は、ゲームオーバー状態であることを伝達する
            if (_mapManager.PlayerHqCount < 1 || _mapManager.EnemyHqCount < 1)
            {
                OnGameOverConditionMet.Invoke();
                break;
            }
        }

        // 各タイムラインの中身を完全クリアにする
        _timeline.Clear();
        _playerTimeline.Clear();
        _enemyTimeline.Clear();
    }

    /// <summary>
    /// タイムラインの集計処理（タイムライン結合・ソート）
    /// </summary>
    public void SortAndCombineTimelines()
    {
        // 1. それぞれのリストを個別にソート (要素数が少ない状態でソートするため高速)
        _playerTimeline.Sort(CompareCommands);
        _enemyTimeline.Sort(CompareCommands);

        // 2. 結合後のジャストサイズでリストを生成 (GC Alloc / メモリ再確保のスパイクを完全に防止)
        int totalCount = _playerTimeline.Count + _enemyTimeline.Count;

        int ptrA = 0;
        int ptrB = 0;

        // 3. マージ処理：両方のリストを比較しながら、小さい順に結合リストへ詰める
        while (ptrA < _playerTimeline.Count && ptrB < _enemyTimeline.Count)
        {
            int compare = CompareCommands(_playerTimeline[ptrA], _enemyTimeline[ptrB]);

            // _playerTimelineの要素の方が時間が早い（または同じ）場合
            if (compare <= 0)
            {
                _timeline.Add(_playerTimeline[ptrA]);
                ptrA++;
            }
            else
            {
                _timeline.Add(_enemyTimeline[ptrB]);
                ptrB++;
            }
        }

        // 4. 残った要素をすべて流し込む (ソート済みなのでそのまま追加)
        while (ptrA < _playerTimeline.Count)
        {
            _timeline.Add(_playerTimeline[ptrA]);
            ptrA++;
        }
        while (ptrB < _enemyTimeline.Count)
        {
            _timeline.Add(_enemyTimeline[ptrB]);
            ptrB++;
        }
    }

    /// <summary>
    /// コマンドを作成する（プレイヤー用）
    /// </summary>
    public TimelineCommand CreatePlayerCommand()
    {
        // 事前チェック
        if (_tileManager.selectedTile.Unit == null ||
            _tileManager.selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
        }

        // Colobusの攻撃を登録時、既に他Colobusによるコマンドが登録されている場合は、情報を参照して同様の内容で登録する
        if (_tileManager.selectedTile.Unit is ColobusUnit)
        {
            var registeredColobusCommand = TryGetPlayerColobusAttackCommand();
            if (registeredColobusCommand != null)
            {
                return new TimelineCommand(
                    Owner.Player,
                    attackerUnit,
                    _tileManager.selectedTile,
                    _tileManager.targetTile,
                    new List<Tile>(registeredColobusCommand.AffectedTiles),
                    registeredColobusCommand.Time
                );   
            }
        }
        // そうでない場合は、普通のコマンド登録を行う
        float time = OnRequestPhaseElapsedTime.Invoke() + attackerUnit.Stats.attackProfile.delay;
        return new TimelineCommand(
            Owner.Player,
            attackerUnit,
            _tileManager.selectedTile,
            _tileManager.targetTile,
            new List<Tile>(_tileManager.targetTiles),
            time
        );    
    }

    /// <summary>
    /// コマンドを作成する（エネミー用）
    /// </summary>
    public TimelineCommand CreateEnemyCommand()
    {
        Tile selectedTile = _mapManager.enemyMapData[0, 0];
        Tile targetTile = _mapManager.playerMapData[4, 4];

        // 事前チェック
        if (selectedTile.Unit == null ||
            selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
        }

        // Colobusの攻撃を登録時、既に他Colobusによるコマンドが登録されている場合は、情報を参照して同様の内容で登録する
        if (selectedTile.Unit is ColobusUnit)
        {
            var registeredColobusCommand = TryGetEnemyColobusAttackCommand();
            if (registeredColobusCommand != null)
            {
                return new TimelineCommand(
                    Owner.Enemy,
                    attackerUnit,
                    selectedTile,
                    targetTile,
                    new List<Tile>(registeredColobusCommand.AffectedTiles),
                    registeredColobusCommand.Time
                );   
            }
        }
        // そうでない場合は、普通のコマンド登録を行う
        List<Tile> affectedTiles = new List<Tile>();
        List<Vector2Int> affectedPositions = attackerUnit.Controller.GetTargetTilePositions(targetTile.Stats.GridPos);
        foreach (Vector2Int pos in affectedPositions)
        {
            Tile tile = _mapManager.GetPlayerTile(pos);

            if (tile == null) continue;
            // 配列（リスト）に保存
            affectedTiles.Add(tile);
        }
        float elapsedTime = UnityEngine.Random.Range(1.0f, 60.0f) + attackerUnit.Stats.attackProfile.delay;
        return new TimelineCommand(
            Owner.Enemy,
            attackerUnit,
            selectedTile,
            targetTile,
            affectedTiles,
            elapsedTime
        );
    }

    /// <summary>
    /// プレイヤーのコマンドを予約する
    /// </summary>
    public void RegisterPlayerCommand(TimelineCommand command)
    {
        // コマンド内容をキューに追加
        _playerTimeline.Add(command);
        // 時間の小さい順にする
        _playerTimeline.Sort(CompareCommands);
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_playerTimeline);
    }

    /// <summary>
    /// エネミーのコマンドを予約する
    /// </summary>
    public void RegisterEnemyCommand(TimelineCommand command)
    {
        // コマンド内容をキューに追加
        _enemyTimeline.Add(command);
    }

    /// <summary>
    /// 共用タイムライン内のコマンド有効性をチェックし、有効なコマンド以外を除外する。（ACTIONフェーズ用）
    /// </summary>
    // public void CheckSharedTimelineCommandValidity()
    // {
    //     for (int i = _timeline.Count - 1; i > 0; i--)
    //     {
    //         if (_timeline[i].AttackerUnit.Stats.IsFaint)
    //         {
    //             RemoveSharedTimelineCommand(i);
    //         }
    //     }
    // }

    /// <summary>
    /// コマンドの有効性をチェックし、有効なコマンド以外を除外する。
    /// </summary>
    public void CheckPlayerTimelineCommandValidity()
    {
        int commandIndex = -1;
        for (int i = 0; i < _playerTimeline.Count; i++)
        {
            Debug.Log($"{_playerTimeline[i].AttackerUnit.Stats.profile.unitName} : {_playerTimeline[i].AttackerUnit.gameObject.activeSelf}");

            if (_playerTimeline[i].AttackerUnit.gameObject.activeSelf == false)
            {
                commandIndex = i;
                // RemovePlayerTimelineCommand(i);
            }
        }

        if (commandIndex > -1)
        { 
            RemovePlayerTimelineCommand(commandIndex);
        }
        else
        {
            new Exception("コマンドの除外に失敗しました。");
        }
    }

    /// <summary>
    /// プレイヤータイムラインにある最初のColobusの攻撃コマンドを取得する（なければnull）
    /// </summary>
    public TimelineCommand TryGetPlayerColobusAttackCommand()
    {
        return _playerTimeline.Find(cmd => cmd.AttackerUnit.Stats.profile.unitType == UnitType.Colobus);
    }

    /// <summary>
    /// 敵タイムラインにある最初のColobusの攻撃コマンドを取得する（なければnull）
    /// </summary>
    public TimelineCommand TryGetEnemyColobusAttackCommand()
    {
        return _enemyTimeline.Find(cmd => cmd.AttackerUnit.Stats.profile.unitType == UnitType.Colobus);
    }

    /// <summary>
    /// 共有タイムラインのコマンドを除外する
    /// </summary>
    private void RemoveSharedTimelineCommand(Predicate<TimelineCommand> match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match), "削除条件 (match) が指定されていません。");
        // 条件に該当するコマンドをキューから除外
        _timeline.RemoveAll(match);
        // 時間の小さい順にする
        _timeline.Sort(CompareCommands);
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_timeline);
    }

    /// <summary>
    /// プレイヤータイムライン内のコマンドを除外する
    /// </summary>
    private void RemovePlayerTimelineCommand(int index)
    {
        // 指定コマンドをキューから除外
        _playerTimeline.RemoveAt(index);
        // 時間の小さい順にする
        _playerTimeline.Sort(CompareCommands);
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_playerTimeline);
    }

    /// <summary>
    /// コマンドの比較用メソッド
    /// </summary>
    private int CompareCommands(TimelineCommand a, TimelineCommand b)
    {
        return a.Time.CompareTo(b.Time);
    }
}
