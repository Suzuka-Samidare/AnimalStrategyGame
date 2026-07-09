using UnityEngine;
using Cysharp.Threading.Tasks;
using TimelineCommand = TimelineManager.TimelineCommand;

public class CombatPerformanceDirector : MonoBehaviour
{
    [Header("発射物共通設定")]
    [SerializeField]
    private float _groundHeight = 0.5f;
    [SerializeField]
    private float _peakHeight = 3f;

    [Header("Refs")]
    private MapManager _mapManager;
    private ParticleManager _particleManager;
    private ProjectileManager _projectileManager;

    private void Start()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _particleManager = ParticleManager.Instance;
        _projectileManager = ProjectileManager.Instance;
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
    }

    /// <summary>
    /// インク攻撃失敗（迎撃された）演出
    /// </summary>
    public async UniTask AttackInkFailed(TimelineCommand command, Vector3 interceptingUnitTilePos, Vector3 interceptedPos)
    {
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
