using UnityEngine;
using Cysharp.Threading.Tasks;
// using MovementPath = TrajectoryCalculator.MovementPath;

public class SquidAttackVisualizer : MonoBehaviour
{
    [Header("インク攻撃設定")]
    [SerializeField]
    private MovementPath _ascentPath;
    // [SerializeField]
    // private float _ascentDuration;
    [SerializeField]
    private float _groundHeight = 0.5f;
    [SerializeField]
    private float _peakHeight = 3f;
    [SerializeField]
    private float _inkSpeed = 7.0f;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject _inkPrefab;
    [SerializeField]
    private GameObject _herringSchoolPrefab;

    [Header("Refs")]
    private MapManager _mapManager;
    private ParticleManager _particleManager;
    private ProjectileManager _projectileManager;
    private SquidAnimation _sqiudAnimation;

    private void Awake()
    {
        _sqiudAnimation = GetComponent<SquidAnimation>();
    }

    private void Start()
    {
        ResolveDependencies();

        _ascentPath.start = transform.position;
        _ascentPath.start.y = 0.5f;
        // TODO: playerからenemyへの攻撃にしか対応してない（ハードコーティング）
        _ascentPath.end = _mapManager.playerMapData[(_mapManager.playerMapData.GetLength(0) - 1) / 2, _mapManager.playerMapData.GetLength(1) - 1].Stats.GlobalPos;
        _ascentPath.end.y = _peakHeight;
        // _ascentDuration = Vector3.Distance(_ascentPath.start, _ascentPath.end) / _inkSpeed;
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _particleManager = ParticleManager.Instance;
        _projectileManager = ProjectileManager.Instance;
    }

    public async UniTask AttackInkSuccess(Vector3 descentFinishPos)
    {
        ProjectlieBase ink = _projectileManager.SpawnProjectile(ProjectileType.Ink, _ascentPath.start, transform.rotation);
        if (ink != null && ink.TryGetComponent<ParabolicMover>(out var parabolicMover))
        {
            Vector3 descentStartPos = new Vector3(descentFinishPos.x, _peakHeight, descentFinishPos.z - 10);
            descentFinishPos.y = 0.5f;

            await CameraMovement.Instance.MoveToAsync(transform.position);
            await _sqiudAnimation.PlayOnceAsync(AnimationName.Attack);
            await parabolicMover.AscendAsync(_ascentPath);
            CameraMovement.Instance.Follow(
                ink.transform,
                () => Vector3.Distance(ink.transform.position, descentFinishPos) < 0.1f
            );
            await parabolicMover.DescentAsync(new MovementPath { start = descentStartPos, end = descentFinishPos });
            _projectileManager.DespawnProjectile(ink);
        }
        else
        {
            throw new System.Exception("ParabolicMoverにアクセスできません。");
        }
    }

    public async UniTask AttackInkFailed(Vector3 descentFinishPos, Vector3 interceptedPos, Vector3 interceptUnitPos)
    {
        // 座標定義
        Vector3 descentStartPos = new Vector3(descentFinishPos.x, _peakHeight, descentFinishPos.z - 10);
        descentFinishPos.y = _groundHeight;
        MovementPath descentPath = new MovementPath { start = descentStartPos, end = descentFinishPos };
        // 各発射物オブジェクトの生成
        ProjectlieBase ink = _projectileManager.SpawnProjectile(
            ProjectileType.Ink,
            _ascentPath.start,
            transform.rotation);
        ProjectlieBase herringSchool = _projectileManager.SpawnProjectile(
            ProjectileType.HerringSchool,
            interceptUnitPos,
            transform.rotation);
        // インクの上昇時間
        float inkDuration = Vector3.Distance(_ascentPath.start, _ascentPath.end) / _inkSpeed;
        // 下降開始から、迎撃Z地点に届くまでの情報を計算
        InterceptTargetInfo interceptInfo = TrajectoryCalculator.CalculateDescentInterceptInfo(
            descentPath,
            _inkSpeed,
            interceptedPos.z
        );
        // インクが発射されてから、迎撃されるまでの総時間
        float totalTimeToIntercept = inkDuration + interceptInfo.TimeToReach;
        // 4. Herring側発射物の上昇時間を計算
        float fishSchoolSpeed = 7.0f;
        float herringSchoolDuration = Vector3.Distance(interceptUnitPos, interceptInfo.Position) / fishSchoolSpeed;
        // 5. fishSchoolを発射するまでの「待ち時間（ディレイ）」を逆算
        float delayBeforeLaunch = totalTimeToIntercept - herringSchoolDuration;

        await CameraMovement.Instance.MoveToAsync(transform.position);
        await _sqiudAnimation.PlayOnceAsync(AnimationName.Attack);

        // 6. 二つの移動処理を並列で実行
        UniTask inkTask = UniTask.Create(async () =>
        {
            if (ink != null && ink.TryGetComponent<ParabolicMover>(out var inkMover))
            {
                // 上昇
                await inkMover.AscendAsync(_ascentPath);
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
        UniTask interceptorTask = LaunchFishSchoolWithDelayAsync(
            herringSchool,
            new MovementPath { start = interceptUnitPos, end = interceptInfo.Position },
            delayBeforeLaunch
        );

        // 両方の移動・演出が終わるまで待機
        await UniTask.WhenAll(inkTask, interceptorTask);
    }

    private async UniTask LaunchFishSchoolWithDelayAsync(ProjectlieBase herringSchool, MovementPath ascentPath, float delay)
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
