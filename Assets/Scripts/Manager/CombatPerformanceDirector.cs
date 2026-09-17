using UnityEngine;
using Cysharp.Threading.Tasks;
using TimelineCommand = TimelineManager.TimelineCommand;
using System.Collections.Generic;
using Unity.VisualScripting;

public class CombatPerformanceDirector : MonoBehaviour
{
    [Header("発射物共通設定")]
    [SerializeField]
    private float _groundHeight = 0.5f;
    [SerializeField]
    private float _peakHeight = 3f;

    [Header("ドローン設定")]
    [SerializeField] private float _dronePeakHeight = 2f;

    [Header("Refs")]
    private MapManager _mapManager;
    private ParticleManager _particleManager;
    private ProjectileManager _projectileManager;
    private CameraMovement _cameraMovement;

    private void Start()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _particleManager = ParticleManager.Instance;
        _projectileManager = ProjectileManager.Instance;
        _cameraMovement = CameraMovement.Instance;
    }

    /// <summary>
    /// 発射物上昇時の放物線軌道の始点及び終点座標の取得
    /// </summary>
    private MovementPath GetAscentPath(Tile attackerTile)
    {
        Tile[,] attackerMapData = attackerTile.Stats.owner == Owner.Player ? _mapManager.playerMapData : _mapManager.enemyMapData;
        MovementPath ascentPath = new()
        {
            start = new Vector3(attackerTile.Stats.GlobalPos.x, _groundHeight, attackerTile.Stats.GlobalPos.z),
            end = attackerMapData[(attackerMapData.GetLength(0) - 1) / 2,  attackerMapData.GetLength(1) - 1].Stats.GlobalPos
        };
        ascentPath.end.y = _peakHeight;

        return ascentPath;
    }

    /// <summary>
    /// 発射物下降時の放物線軌道の始点及び終点座標の取得
    /// </summary>
    private MovementPath GetDescentPath(Tile targetTile)
    {
        Vector3 tgtTileGlobalPos = targetTile.Stats.GlobalPos;
        float bufferDistance = 5f;
        float frontlinePosZ = targetTile.Stats.owner == Owner.Player
            ? _mapManager.GetPlayerTile(new Vector2Int(targetTile.Stats.GridPos.x, _mapManager.TotalMapHeight - 1), true).Stats.GlobalPos.z + bufferDistance
            : _mapManager.GetEnemyTile(new Vector2Int(targetTile.Stats.GridPos.x, _mapManager.TotalMapHeight - 1), true).Stats.GlobalPos.z - bufferDistance;
        MovementPath descentPath = new()
        {
            start = new Vector3(tgtTileGlobalPos.x, _peakHeight, frontlinePosZ),
            end = new Vector3(tgtTileGlobalPos.x, _groundHeight, tgtTileGlobalPos.z),
        };

        return descentPath;
    }

    /// <summary>
    /// インク攻撃成功演出
    /// </summary>
    public async UniTask AttackInkSuccess(TimelineCommand command)
    {
        // TODO: ここでIsVisibleすべきでない
        bool isVisibleAttacker = command.AttackerTile.Unit.Stats.IsVisible;
        if (!isVisibleAttacker) command.AttackerTile.Unit.SetVisible(true);

        if (command.AttackerTile.Unit is not SquidUnit squid)
        {
            throw new System.InvalidOperationException("攻撃演出を実行できません：登録されているユニットはSquidではありません。");
        }

        MovementPath ascentPath = GetAscentPath(command.AttackerTile);
        MovementPath descentPath = GetDescentPath(command.TargetTile);
        ProjectlieBase ink = _projectileManager.SpawnProjectile(
            ProjectileType.Ink,
            ascentPath.start,
            transform.rotation);
        if (ink != null)
        {
            // Vector3 tgtTileGlobalPos = command.TargetTile.Stats.GlobalPos;
            // MovementPath descentPath = new()
            // {
            //     start = new Vector3(tgtTileGlobalPos.x, _peakHeight, tgtTileGlobalPos.z - 10),
            //     end = new Vector3(tgtTileGlobalPos.x, _groundHeight, tgtTileGlobalPos.z),
            // };
            ParabolicMover parabolicMover = ink.GetComponent<ParabolicMover>();

            await CameraMovement.Instance.MoveToAsync(squid.transform.position);
            await squid.Animation.PlayOnceAsync(AnimationName.Attack);
            await parabolicMover.AscendAsync(ascentPath);
            CameraMovement.Instance.Follow(
                ink.transform,
                () => Vector3.Distance(ink.transform.position, descentPath.end) < 0.1f
            );
            await parabolicMover.DescentAsync(descentPath);
            _projectileManager.DespawnProjectile(ink);
        }

        if (!isVisibleAttacker) command.AttackerTile.Unit.SetVisible(false);
    }

    /// <summary>
    /// インク攻撃失敗（迎撃された）演出
    /// </summary>
    public async UniTask AttackInkFailed(TimelineCommand command, Vector3 interceptingUnitTilePos, Vector3 interceptedPos)
    {
        bool isVisibleAttacker = command.AttackerTile.Unit.Stats.IsVisible;
        if (!isVisibleAttacker) command.AttackerTile.Unit.SetVisible(true);

        if (command.AttackerTile.Unit is not SquidUnit squid)
        {
            throw new System.InvalidOperationException("攻撃演出を実行できません：登録されているユニットはSquidではありません。");
        }

        MovementPath ascentPath = GetAscentPath(command.AttackerTile);
        MovementPath descentPath = GetDescentPath(command.TargetTile);
        // Vector3 tgtTileGlobalPos = command.TargetTile.Stats.GlobalPos;
        // MovementPath descentPath = new()
        // {
        //     start = new Vector3(tgtTileGlobalPos.x, _peakHeight, tgtTileGlobalPos.z - 10),
        //     end = new Vector3(tgtTileGlobalPos.x, _groundHeight, tgtTileGlobalPos.z)
        // };

        // 各発射物オブジェクトの生成
        ProjectlieBase ink = _projectileManager.SpawnProjectile(
            ProjectileType.Ink,
            ascentPath.start,
            transform.rotation);
        ProjectlieBase herringSchool = _projectileManager.SpawnProjectile(
            ProjectileType.HerringSchool,
            interceptingUnitTilePos,
            transform.rotation);
        // インクスピード
        float inkSpeed = 7.0f;
        // インクの上昇時間
        float inkDuration = Vector3.Distance(ascentPath.start, ascentPath.end) / inkSpeed;
        // 下降開始から、迎撃Z地点に届くまでの情報を計算
        InterceptTargetInfo interceptInfo = TrajectoryCalculator.CalculateDescentInterceptInfo(
            descentPath,
            inkSpeed,
            interceptedPos.z
        );
        // インクが発射されてから、迎撃されるまでの総時間
        float totalTimeToIntercept = inkDuration + interceptInfo.TimeToReach;
        // 4. Herring側発射物の上昇時間を計算
        float fishSchoolSpeed = 7.0f;
        float herringSchoolDuration = Vector3.Distance(interceptingUnitTilePos, interceptInfo.Position) / fishSchoolSpeed;
        // 5. fishSchoolを発射するまでの「待ち時間（ディレイ）」を逆算
        float delayBeforeLaunch = totalTimeToIntercept - herringSchoolDuration;

        await CameraMovement.Instance.MoveToAsync(squid.transform.position);
        await squid.Animation.PlayOnceAsync(AnimationName.Attack);

        // 6. 二つの移動処理を並列で実行
        UniTask inkTask = UniTask.Create(async () =>
        {
            if (ink != null && ink.TryGetComponent<ParabolicMover>(out var inkMover))
            {
                // 上昇
                await inkMover.AscendAsync(ascentPath);
                // 下降（タイミングが来たら内部で消失し、コールバックを呼ぶ）
                CameraMovement.Instance.Follow(
                    ink.transform,
                    () => Vector3.Distance(ink.transform.position, interceptedPos) < 0.1f
                );
                await inkMover.DescentWithInterruptAsync(
                    descentPath,
                    interceptInfo,
                    async (pos) =>
                    {
                        _projectileManager.DespawnProjectile(ink);
                        await _particleManager.PerformFireExplosionAsync(pos, Quaternion.identity);
                        await UniTask.CompletedTask;
                    }
                );
            }
            else
            {
                throw new System.Exception("Ink: のParabolicMoverにアクセスできません。");
            }
        });
        UniTask interceptorTask = LaunchHerringSchoolWithDelayAsync(
            herringSchool,
            new MovementPath { start = interceptingUnitTilePos, end = interceptInfo.Position },
            delayBeforeLaunch
        );

        // 両方の移動・演出が終わるまで待機
        await UniTask.WhenAll(inkTask, interceptorTask);

        if (!isVisibleAttacker) command.AttackerTile.Unit.SetVisible(false);
    }

    /// <summary>
    /// Colobusの輸送演出
    /// </summary>
    public async UniTask AirdropColobus(List<TimelineCommand> commands, Tile[] arrivalTiles)
    {
        // ガード処理＆Colobusユニットの参照リストを用意
        List<ColobusUnit> colobusUnits = new List<ColobusUnit>(commands.Count);
        foreach (var command in commands)
        {  
            if (command.AttackerUnit is not ColobusUnit colobus)
            {
                throw new System.InvalidOperationException("攻撃演出を実行できません：登録されているユニットはSquidではありません。");
            }
            colobusUnits.Add(colobus);
        }

        // ユニット参照関連の変数
        TimelineCommand masterCmd = commands[0];
        ColobusUnit masterColobus = colobusUnits[0];

        // 数値関連の変数
        float playerMapFrontlinePosZ = _mapManager.GetPlayerTile(new Vector2Int(0, _mapManager.TotalMapHeight - 1), true).Stats.GlobalPos.z;
        float enemyMapFrontlinePosZ = _mapManager.GetEnemyTile(new Vector2Int(0, _mapManager.TotalMapHeight - 1), true).Stats.GlobalPos.z;
        float attackerMapFrontlinePosZ = masterCmd.Owner == Owner.Player ? playerMapFrontlinePosZ : enemyMapFrontlinePosZ;
        float defenderMapFrontlinePosZ = masterCmd.Owner == Owner.Player ? enemyMapFrontlinePosZ : playerMapFrontlinePosZ;
        Vector3 masterColobusGlobalPos = masterColobus.transform.position;
        Vector3 targetGlobalPos = masterCmd.TargetTile.Stats.GlobalPos + new Vector3(0f, _groundHeight, 0f);
        MovementPath ascentDronePath = new ()
        {
            start = masterColobusGlobalPos + Vector3.up,
            end = masterColobusGlobalPos + Vector3.up + new Vector3(0f, _dronePeakHeight, 0f)
        };
        MovementPath outgoingDronePath = new ()
        {
            start = ascentDronePath.end,
            end = new Vector3(masterColobusGlobalPos.x, _dronePeakHeight + 1f, attackerMapFrontlinePosZ)
        };
        MovementPath incomingDronePath = new ()
        {
            start = new Vector3(targetGlobalPos.x, _dronePeakHeight + 1f, defenderMapFrontlinePosZ),
            end = targetGlobalPos + Vector3.up + new Vector3(0f, _dronePeakHeight, 0f)
        };
        MovementPath leaveDronePath = new ()
        {
            start = incomingDronePath.end,
            end = targetGlobalPos + new Vector3(0f, 5f, 0f)
        };
        // ドローン参照関連の変数
        var drone = _projectileManager.SpawnProjectile(
            ProjectileType.Drone,
            masterColobusGlobalPos + new Vector3(0f, 10f, 0f),
            transform.rotation
        );
        var droneLinearMover = drone.GetComponent<LinearMover>();

        // colobus用非同期タスク管理用リスト
        UniTask[] colobusTasks = new UniTask[colobusUnits.Count];

        // ドローン登場
        await _cameraMovement.MoveToAsync(masterColobusGlobalPos);
        await droneLinearMover.MoveToAsync(drone.transform.position, ascentDronePath.start);
        // 各ユニットをマスターに集結
        for (int i = 0; i < colobusUnits.Count; i++)
        {   
            ColobusUnit colobus = colobusUnits[i];
            colobusTasks[i] = colobus.LinearMover.MoveToAsync(colobus.transform.position, masterCmd.AttackerUnit.transform.position);
        }
        await UniTask.WhenAll(colobusTasks);
        // ユニットのドローン追従設定有効化
        foreach (var colobus in colobusUnits)
        {
            colobus.PositionFollower.SetTarget(drone.transform);
        }
        await _cameraMovement.MoveToAsync(drone.transform.position);
        // ドローン浮上
        await droneLinearMover.MoveToAsync(ascentDronePath.start, ascentDronePath.end);
        // ドローンの味方マップ内移動
        await droneLinearMover.MoveToAsync(outgoingDronePath.start, outgoingDronePath.end);
        // 敵マップ上での演出に移行準備
        drone.transform.position = incomingDronePath.start;
        for (int i = 0; i < colobusUnits.Count; i++)
        {   
            ColobusUnit colobus = colobusUnits[i];
            colobusTasks[i] = colobus.PositionFollower.WaitArrivalAsync();
        }
        await UniTask.WhenAll(colobusTasks);
        // ドローンの敵マップ内移動
        _cameraMovement.Follow(drone.transform, () => Vector3.Distance(drone.transform.position, targetGlobalPos) < 0.1f);
        await droneLinearMover.MoveToAsync(incomingDronePath.start, incomingDronePath.end);

        // ユニットのドローン追従設定解除 & 各自着地
        for (int i = 0; i < colobusUnits.Count; i++)
        {   
            ColobusUnit colobus = colobusUnits[i];
            Tile arrivalTile = arrivalTiles[i];
            colobus.PositionFollower.ClearTarget();
            colobusTasks[i] = colobus.LinearMover.MoveToAsync(colobus.transform.position, arrivalTile.Stats.GlobalPos + colobus.Stats.profile.InitPos);
        }
        await UniTask.WhenAll(colobusTasks);
        // ドローン退場
        await _cameraMovement.MoveToAsync(masterColobus.transform.position);
        await droneLinearMover.MoveToAsync(leaveDronePath.start, leaveDronePath.end);
        _projectileManager.DespawnProjectile(drone);

        // 一時的な位置リセット動作
        foreach (var command in commands)
        {
            command.AttackerUnit.transform.position = command.AttackerTile.Stats.GlobalPos + command.AttackerUnit.Stats.profile.InitPos;
        }
    }

    /// <summary>
    /// 迎撃地点に合わせてニシンを発射する演出処理
    /// </summary>
    private async UniTask LaunchHerringSchoolWithDelayAsync(ProjectlieBase herringSchool, MovementPath ascentPath, float delay)
    {
        if (delay > 0)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
        }

        if (herringSchool != null && herringSchool.TryGetComponent<ParabolicMover>(out var herringSchoolMover))
        {
            Vector3 startToEnd = ascentPath.end - ascentPath.start;
            Vector3 descentEnd = ascentPath.end + new Vector3(startToEnd.x, -startToEnd.y, startToEnd.z);
            MovementPath descentPath = new MovementPath{ start = ascentPath.end, end = descentEnd };
            // 迎撃側の魚の群れが、ターゲット（交差ポイント）に向かって上昇しながら向かう演出
            await herringSchoolMover.AscendAsync(ascentPath);
            // 到着したら消失
            await herringSchoolMover.DescentAsync(descentPath);
            _projectileManager.DespawnProjectile(herringSchool);
        }
        else
        {
            throw new System.Exception("Herring: ParabolicMoverにアクセスできません。");
        }
    }
}
