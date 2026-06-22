using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TileOwner = TileController.TileOwner;

public class TimelineManager : MonoBehaviour, IInitializable
{
    public static TimelineManager Instance { get; private set; }

    // コマンド情報をまとめたクラス
    [System.Serializable]
    public class TimelineCommand
    {
        public TileOwner Owner;
        public string UnitName;
        public TileController Attacker;
        public TileController Target;  // 攻撃対象の中心タイル
        public List<TileController> AffectedTiles; 
        public float Damage;        // ダメージ量
        public float time; // 経過時間 + 適用必要時間

        public TimelineCommand(
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
    private async Task ExecuteCommandAsync(TimelineCommand command)
    {
        // 防衛が成功したか
        bool isSuccessDefence;
        // 迎撃完了座標（グリッド）
        Vector2Int interceptedGridPos = Vector2Int.zero;
        // 迎撃完了座標（グローバル）
        Vector3 interceptedPos = Vector3.zero;
        // 防衛処理
        if (command.Owner == TileOwner.Player)
        {
            isSuccessDefence = _attackManager.GetEnemyDefenceResult(command, out interceptedGridPos);
            TileController interceptedTile = _mapManager.GetEnemyTile(interceptedGridPos, true);
            interceptedPos = interceptedTile.GlobalPos;
        }
        else
        {
            // isSuccessDefence = GetPlayerDefenceResult();
            isSuccessDefence = false;
        }

        // 防衛に失敗している場合、内部的なダメージの反映（見た目に反映されないAPI通信に近い更新）
        if (!isSuccessDefence)
        {
            await _attackManager.ApplyDamage(command);
        }

        Debug.Log($"isSuccessDefence: {isSuccessDefence}");

        // ===================================================
        // 見た目の演出
        // ===================================================
        // 防錆成功 => 攻撃が迎撃される演出のみ
        // 防衛失敗 => 攻撃ヒット及びユニットの気絶演出
        if (command.Attacker.unitObject.TryGetComponent<SquidController>(out var squidController))
        {
            await squidController.AttackInkSuccess(command.Target.GlobalPos, interceptedPos);
        }
        // 防衛に失敗している場合は、攻撃ヒット及びユニットの気絶演出
        if (!isSuccessDefence)
        {
            await _attackManager.AttackHitEffects(command);
            await _attackManager.FaintEffects(command);
        }
    }

    /// <summary>
    /// コマンドを予約する（外部から呼ぶ）
    /// </summary>
    public void RegisterCommand()
    {
        UnitProfile profile = _tileManager.selectedTileController.UnitBase.Stats.profile;
        AttackProfile attackProfile = _tileManager.selectedTileController.UnitAttackable.Stats.profile;
        // 攻撃内容を作成してキューに追加
        TimelineCommand newAttack = new TimelineCommand(
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
