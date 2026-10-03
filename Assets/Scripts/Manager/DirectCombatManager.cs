using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TimelineCommand = TimelineManager.TimelineCommand;

public class DirectCombatManager : MonoBehaviour
{
    [Header("戦闘設定")]
    [SerializeField] private float _timeLimit = 30f;

    [Header("処理関連")]
    [SerializeField] private int _maxAttackerCapacity = 3;
    [SerializeField] private int _maxDefencerCapacity = 24;
    [SerializeField] private List<Tile> attackerUnitTiles;
    [SerializeField] private List<Tile> defenderUnitTiles;

    [Header("Refs")]
    [SerializeField] private InfomationController _infomationController;
    private readonly TimeoutController _timeoutController = new TimeoutController();

    private void Reset()
    {
        if (_infomationController == null) _infomationController = FindFirstObjectByType<InfomationController>();
    }

    private void Awake()
    {
        Debug.Assert(
            _infomationController != null, 
            $"[アサイン漏れ] {gameObject.name} (コンポーネント: {GetType().Name}) の '_infoController' が未設定です。ヒエラルキー上に 'InfomationController' を配置してください。", 
            this
        );
    }

    private void OnDestroy()
    {
        _timeoutController.Dispose();
    }

    public async UniTask StartCombatAsync(List<TimelineCommand> commands)
    {
        // ユニット関連
        attackerUnitTiles = GetAttackerUnitTiles(commands);
        defenderUnitTiles = GetDefenderUnitTiles(commands);
        // タイムリミットトークン
        var timeoutToken = _timeoutController.Timeout(TimeSpan.FromSeconds(_timeLimit));
        // どちらかの陣営が全滅した時のCTS
        using var defeatedCts = new CancellationTokenSource();
        // 上記２つのトークンを結合したCTSとトークン本体
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, defeatedCts.Token);
        var combatToken = linkedCts.Token;
        // 交戦タスクリスト
        var combatTasks = new List<UniTask>();

        // 双方の交戦可能ユニットのループ処理を実行
        foreach (var tile in attackerUnitTiles)
        {
            if (tile.IsExistUnit && tile.Unit.Controller is ICombatable combatableUnit)
            {
                combatTasks.Add(combatableUnit.RunCombatLoopAsync(tile, defenderUnitTiles, combatToken));
            }
        }
        foreach (var tile in defenderUnitTiles)
        {
            if (tile.IsExistUnit && tile.Unit.Controller is ICombatable combatableUnit)
            {
                combatTasks.Add(combatableUnit.RunCombatLoopAsync(tile, attackerUnitTiles, combatToken));
            }
        }

        // 全滅判定をバックグラウンドで監視するタスクを追加
        combatTasks.Add(WatchDefeatConditionAsync(defeatedCts, combatToken));
        
        // 全タスクの並行実行（時間切れ or 全滅で linkedCts が発火して終了）
        try
        {
            await UniTask.WhenAll(combatTasks);
        }
        catch (OperationCanceledException)
        {
            if (IsAnyFactionDefeated())
            {
                await _infomationController.OpenAsync("Combat Ended.");
            }
            else
            {
                await _infomationController.OpenAsync("Time Up !!");
            }
        }
        finally
        {
            _timeoutController.Reset();
            ResetAllCombatState();
        }
    }

    /// <summary>
    /// 両陣営のユニット生存状態を毎フレーム監視し、全滅を検知したら CancellationTokenSource をキャンセルする
    /// </summary>
    private async UniTask WatchDefeatConditionAsync(CancellationTokenSource cts, CancellationToken combatToken)
    {
        while (!combatToken.IsCancellationRequested)
        {
            // どちらかの全ユニットの気絶を検知したら全滅用CTSをキャンセル
            if (IsAnyFactionDefeated())
            {
                cts.Cancel();
                break;
            }
            // 毎フレームチェック
            await UniTask.Yield(PlayerLoopTiming.Update, combatToken);
        }
    }

    /// <summary>
    /// どちらかの陣営が全滅（生存ユニット数が0、または全員気絶）しているか判定
    /// </summary>
    private bool IsAnyFactionDefeated()
    {
        Predicate<Tile> isFaintOrNull = tile => tile.Unit == null || tile.Unit.Stats.IsFaint;
        bool attackerAllFaint = attackerUnitTiles.TrueForAll(isFaintOrNull);
        bool defenderAllFaint = defenderUnitTiles.TrueForAll(isFaintOrNull);

        return attackerAllFaint || defenderAllFaint;
    }

    private void ResetAllCombatState()
    {
        foreach (var tile in attackerUnitTiles)
        {
            if (tile.Unit != null && tile.Unit is ICombatable combatableUnit)
            {
                combatableUnit.RecastTimer.Reset();
                combatableUnit.IsAttacking = false;
                combatableUnit.IsAttackReady = true;
            }
        }
        foreach (var tile in defenderUnitTiles)
        {
            if (tile.Unit != null && tile.Unit is ICombatable combatableUnit)
            {
                combatableUnit.RecastTimer.Reset();
                combatableUnit.IsAttacking = false;
                combatableUnit.IsAttackReady = true;
            }
        }
    }

    /// <summary>
    /// 攻撃側ユニットリスト取得
    /// </summary>
    private List<Tile> GetAttackerUnitTiles(List<TimelineCommand> commands)
    {
        var tiles = new List<Tile>(_maxAttackerCapacity);
        foreach (var command in commands)
        {
            tiles.Add(command.AttackerTile);
        }
        return tiles;
    }

    /// <summary>
    /// 守備側ユニットリストの取得
    /// </summary>
    private List<Tile> GetDefenderUnitTiles(List<TimelineCommand> commands)
    {
        TimelineCommand command = commands[0];
        var tiles = new List<Tile>(_maxDefencerCapacity);
        foreach (var tile in command.AffectedTiles)
        {  
            if (tile.IsExistUnit) tiles.Add(tile);
        }
        return tiles;
    }


    // / <summary>
    // / 局地戦のセットアップ処理
    // / </summary>
    // public async UniTask ExecuteDirectCombatAsync(List<TimelineCommand> commands, CancellationToken cancellationToken = default)
    // {
    //     // ================================================
    //     // 各種データ
    //     // ================================================
    //     // タイマー関連
    //     bool isTimeUp = false;
    //     Timer timer = new Timer();
    //     Action onTimeUpAction = () => isTimeUp = true;
    //     // ユニット関連
    //     List<UnitBase> attackerUnits = GetAttackerUnits(commands);
    //     List<UnitBase> defencerUnits = GetDefencerUnits(commands);
    //     List<Tile> attackerTiles = GetAttackerTiles(commands);
    //     List<Tile> defencerTiles = GetDefencerTiles(commands);

    //     // ================================================
    //     // initialize
    //     // ================================================
    //     // タイマー処理
    //     timer.OnTimerComplete += onTimeUpAction;
    //     timer.Start(_timeLimit);

    //     // ================================================
    //     // 交戦処理
    //     // ================================================
    //     try
    //     {
    //         // 余裕をもってカバーできそうな16で事前確保
    //         var tasks = new List<UniTask>(16);

    //         while (!isTimeUp && !cancellationToken.IsCancellationRequested)
    //         {   
    //             // タスクリストのリセット
    //             tasks.Clear();
    //             // 交戦時間の更新
    //             timer.UpdateTick(Time.deltaTime);
    //             // 制限時間切れ及び片方の勢力が全滅した時の判定
    //             if (isTimeUp
    //                 || CheckUnitsDefeated(attackerTiles)
    //                 || CheckUnitsDefeated(defencerTiles))
    //             {
    //                 Debug.Log($"AttackerDefeated: {CheckUnitsDefeated(attackerTiles)}");
    //                 Debug.Log($"DefencerDefeated: {CheckUnitsDefeated(defencerTiles)}");
    //                 break;
    //             }

    //             // 攻撃側ユニットの処理
    //             for (int i = 0; i < attackerTiles.Count; i++)
    //             {
    //                 var tile = attackerTiles[i];
    //                 var unit = tile.Unit;
    //                 // ユニットが既に気絶していて存在しない場合は処理スキップ
    //                 if (!tile.IsExistUnit) continue;
    //                 // 気絶フラグがあれば、ユニットデスポーン処理を実行
    //                 if (unit.Stats.IsFaint)
    //                 {
    //                     tasks.Add(unit.OnFaint(tile));
    //                     _hasFaintedUnits = true;
    //                     continue;
    //                 }
    //                 // 攻撃処理の実行
    //                 if (unit is AttackerUnitBase attackerUnit)
    //                 {
    //                     tasks.Add(attackerUnit.Controller.ExecuteCombat(defencerUnits));
    //                 }
    //             }
    //             // 防衛側ユニットの処理
    //             for (int i = 0; i < defencerTiles.Count; i++)
    //             {
    //                 var tile = defencerTiles[i];
    //                 var unit = tile.Unit;
    //                 // ユニットが既に気絶していて存在しない場合は処理スキップ
    //                 if (!tile.IsExistUnit) continue;
    //                 // 気絶フラグがあれば、ユニットデスポーン処理を実行
    //                 if (unit.Stats.IsFaint)
    //                 {
    //                     tasks.Add(unit.OnFaint(tile));
    //                     _hasFaintedUnits = true;
    //                     continue;
    //                 }
    //                 if (unit is AttackerUnitBase attackerUnit)
    //                 {
    //                     tasks.Add(attackerUnit.Controller.ExecuteCombat(attackerUnits));
    //                 }
    //                 if (unit is DefencerUnitBase defencerUnit)
    //                 {
    //                     tasks.Add(defencerUnit.Controller.ExecuteCombat(attackerUnits));
    //                 }
    //             }
    //             // タスクがある場合、全処理が終わるまで待機
    //             if (tasks.Count > 0) await UniTask.WhenAll(tasks);
    //             // 気絶ユニットがいた場合は、タイムラインのコマンド有効性をチェック（コマンド除去処理）
    //             if (_hasFaintedUnits) TimelineManager.Instance.RemoveSharedTimelineCommand(cmd => cmd.AttackerTile.Unit == null);
    //             // 1フレーム待機して時間を進める
    //             await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
    //         }

    //         Debug.Log($"isTimeUp: {isTimeUp}");
    //         Debug.Log($"cancellationToken.IsCancellationRequested: {cancellationToken.IsCancellationRequested}");
    //     }
    //     finally
    //     {
    //         // 戦闘終了のお知らせ
    //         await _infomationController.OpenAsync("Combat Ended.");
    //         // タイマーの後処理
    //         timer.OnTimerComplete -= onTimeUpAction;
    //         timer.Reset();
    //     }
    // }

    // /// <summary>
    // /// 攻撃側ユニットリスト取得
    // /// </summary>
    // private List<UnitBase> GetAttackerUnits(List<TimelineCommand> commands)
    // {
    //     List<UnitBase> unitList = new List<UnitBase>(_maxAttackerCapacity);
    //     foreach (var command in commands)
    //     {  
    //         if (command.AttackerUnit is not ColobusUnit _)
    //         {
    //             throw new InvalidOperationException("攻撃演出を実行できません：登録されているユニットはSquidではありません。");
    //         }
    //         unitList.Add(command.AttackerUnit);
    //     }
    //     return unitList;
    // }

    // /// <summary>
    // /// 守備側ユニットリストの取得
    // /// </summary>
    // private List<UnitBase> GetDefenderUnits(List<TimelineCommand> commands)
    // {
    //     TimelineCommand command = commands[0];
    //     List<UnitBase> unitList = new List<UnitBase>(_maxDefencerCapacity);
    //     foreach (var tile in command.AffectedTiles)
    //     {  
    //         if (tile.IsExistUnit)
    //         {
    //             unitList.Add(tile.Unit);
    //             // tile.Unit.SetVisible(true);
    //         }
    //     }
    //     return unitList;
    // }

    // /// <summary>
    // /// 攻撃側タイルリスト（ユニット参照可能）の取得
    // /// </summary>
    // private List<Tile> GetAttackerTiles(List<TimelineCommand> commands)
    // {
    //     var tileList = new List<Tile>(_maxAttackerCapacity);
    //     foreach (var command in commands)
    //     {  
    //         if (command.AttackerUnit is not ColobusUnit _)
    //         {
    //             throw new InvalidOperationException("攻撃演出を実行できません：登録されているユニットはSquidではありません。");
    //         }
    //         tileList.Add(command.AttackerTile);
    //     }
    //     return tileList;
    // }

    // /// <summary>
    // /// 守備側タイルリスト（ユニット参照可能）の取得
    // /// </summary>
    // private List<Tile> GetDefencerTiles(List<TimelineCommand> commands)
    // {
    //     TimelineCommand command = commands[0];
    //     var tileList = new List<Tile>(_maxDefencerCapacity);
    //     foreach (var tile in command.AffectedTiles)
    //     {  
    //         if (tile.IsExistUnit)
    //         {
    //             tileList.Add(tile);
    //             // tile.Unit.SetVisible(true);
    //         }
    //     }
    //     return tileList;
    // }

    // /// <summary>
    // /// 軍勢ごとのユニット生存チェック
    // /// </summary>
    // private bool CheckUnitsDefeated(List<Tile> Tiles)
    // {
    //     if (Tiles == null || Tiles.Count == 0) return true;

    //     foreach(var tile in Tiles)
    //     {
    //         if (tile.IsExistUnit) return false;
    //     }
    //     return true;
    // }

}
