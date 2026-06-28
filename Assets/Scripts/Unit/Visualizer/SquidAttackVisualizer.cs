using UnityEngine;
using Cysharp.Threading.Tasks;
using MovementPath = ParabolicMover.MovementPath;

public class SquidAttackVisualizer : MonoBehaviour
{
    [Header("インク攻撃設定")]
    [SerializeField]
    private Vector3 _ascentStartPos;
    [SerializeField]
    private Vector3 _ascentFinishPos;
    [SerializeField]
    private float _peakHeight = 3f;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject _inkPrefab;
    [SerializeField]
    private GameObject _herringSchoolPrefab;

    [Header("Refs")]
    private MapManager _mapManager;
    private ParticleManager _particleManager;
    private UnitAnimation _unitAnimation;

    private void Awake()
    {
        _unitAnimation = GetComponent<UnitAnimation>();
    }

    private void Start()
    {
        ResolveDependencies();

        _ascentStartPos = transform.position;
        _ascentStartPos.y = 0.5f;
        // TODO: playerからenemyへの攻撃にしか対応してない（ハードコーティング）
        _ascentFinishPos = _mapManager.playerMapData[(_mapManager.playerMapData.GetLength(0) - 1) / 2, _mapManager.playerMapData.GetLength(1) - 1].GlobalPos;
        _ascentFinishPos.y = _peakHeight;
    }

    private void ResolveDependencies()
    {
        _mapManager = MapManager.Instance;
        _particleManager = ParticleManager.Instance;
    }

    public async UniTask AttackInkSuccess(Vector3 descentFinishPos)
    {
        GameObject ink = Instantiate(_inkPrefab, _ascentStartPos, transform.rotation);
        if (ink != null && ink.TryGetComponent<ParabolicMover>(out var parabolicMover))
        {
            Vector3 descentStartPos = new Vector3(descentFinishPos.x, _peakHeight, descentFinishPos.z - 10);
            descentFinishPos.y = 0.5f;

            await CameraMovement.Instance.MoveToAsync(transform.position);
            await _unitAnimation.PlayOnceAsync(AnimationName.Attack);
            await parabolicMover.AscendAsync(new MovementPath { start = _ascentStartPos, end = _ascentFinishPos });
            CameraMovement.Instance.Follow(
                ink.transform,
                () => Vector3.Distance(ink.transform.position, descentFinishPos) < 0.1f
            );
            await parabolicMover.DescentAsync(new MovementPath { start = descentStartPos, end = descentFinishPos });
        }
        else
        {
            throw new System.Exception("ParabolicMoverにアクセスできません。");
        }
    }

    public async UniTask AttackInkFailed(Vector3 descentFinishPos, Vector3 interceptedPos, Vector3 interceptUnitPos)
    {
        GameObject ink = Instantiate(_inkPrefab, _ascentStartPos, transform.rotation);
        GameObject herringSchool = Instantiate(_herringSchoolPrefab, interceptUnitPos, transform.rotation);

        if (ink != null &&
            ink.TryGetComponent<ParabolicMover>(out var inkMover) &&
            herringSchool.TryGetComponent<ParabolicMover>(out var herringSchoolMover))
        {
            Vector3 descentStartPos = new Vector3(descentFinishPos.x, _peakHeight, descentFinishPos.z - 10);
            descentFinishPos.y = 0.5f;

            await CameraMovement.Instance.MoveToAsync(transform.position);
            await _unitAnimation.PlayOnceAsync(AnimationName.Attack);
            await inkMover.AscendAsync(new MovementPath { start = _ascentStartPos, end = _ascentFinishPos });
            CameraMovement.Instance.Follow(
                ink.transform,
                () => Vector3.Distance(ink.transform.position, interceptedPos) < 0.1f
            );
            await inkMover.DescentWithInterruptAsync(
                new MovementPath { start = descentStartPos, end = descentFinishPos },
                interceptedPos,
                async (pos) => await _particleManager.PerformFireExplosionAsync(pos, Quaternion.identity),
                () => herringSchoolMover.AscendAsync(new MovementPath { start = interceptUnitPos, end = interceptedPos }).Forget()
            );
        }
        else
        {
            throw new System.Exception("ParabolicMoverにアクセスできません。");
        }
    }
}
