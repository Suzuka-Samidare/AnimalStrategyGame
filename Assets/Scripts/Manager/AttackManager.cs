using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        public List<TileController> Targets;    // 攻撃対象の中心タイル
        public float Damage;        // ダメージ量
        public float time; // 経過時間 + 適用必要時間

        public AttackCommand(TileOwner owner, string unitName, List<TileController> tiles, float damage, float delay)
        {
            Owner = owner;
            UnitName = unitName;
            Targets = tiles;
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
        // 演出を管理するタスクのリストを用意（後で全部終わったかチェックするため）
        List<Task> animationTasks = new List<Task>();

        foreach (var target in command.Targets)
        {
            if (target.isExistUnit)
            {
                Task damageTask = target.UnitBase.Controller.ApplyDamageAsync(command.Damage, target);
                // あとで一括待機するためにリストに入れておく
                animationTasks.Add(damageTask);
            }
            else
            {
                // TODO: MISS表記を入れたい -> ユニットによる位置基準ではダメ
                // TODO: MISS表記しない場合 -> ACTION中に攻撃範囲を分かるようにしたい
                Debug.Log("ターゲットがもういないみたい。攻撃スカった！");
            }
        }

        await Task.WhenAll(animationTasks);
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
