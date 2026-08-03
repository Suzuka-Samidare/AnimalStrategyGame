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

        public TimelineCommand(
            Owner owner,
            AttackerUnitBase attackerUnit,
            Tile attackerTile,
            Tile targetTile,
            List<Tile> affectedTiles,
            float elapsedTime
        ){
            Owner = owner;
            AttackerUnit = attackerUnit;
            UnitName = attackerUnit.Stats.profile.unitName;
            AttackerTile = attackerTile;
            TargetTile = targetTile;
            AffectedTiles = affectedTiles;
            Damage = attackerUnit.Stats.attackProfile.power;
            Time = elapsedTime + attackerUnit.Stats.attackProfile.delay;
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
    public async UniTask ProcessTimeline()
    {
        _timelinePresenter.UpdateTimeline(_timeline);

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
        if (_tileManager.selectedTile.Unit == null ||
            _tileManager.selectedTile.Unit is not AttackerUnitBase attackerUnit)
        {
            throw new InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
        }

        float elapsedTime = OnRequestPhaseElapsedTime.Invoke();

        return new TimelineCommand(
            Owner.Player,
            attackerUnit,
            _tileManager.selectedTile,
            _tileManager.targetTile,
            _tileManager.targetTiles,
            elapsedTime
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
            throw new InvalidOperationException("コマンドを登録できません：有効な攻撃ユニットが設置されていません。");
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

        float elapsedTime = UnityEngine.Random.Range(1.0f, 60.0f);

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
    /// コマンドを除外する
    /// </summary>
    private void RemoveCommand(int index)
    {
        // 指定コマンドをキューから除外
        _timeline.RemoveAt(index);
        // 時間の小さい順にする
        _timeline.Sort(CompareCommands);
        // タイムラインUIの更新
        _timelinePresenter.UpdateTimeline(_timeline);
    }

    /// <summary>
    /// コマンドの比較用メソッド
    /// </summary>
    private int CompareCommands(TimelineCommand a, TimelineCommand b)
    {
        return a.Time.CompareTo(b.Time);
    }
}
