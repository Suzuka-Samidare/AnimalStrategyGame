using UnityEngine;

public class MoveCamera : MonoBehaviour, IButtonAction
{
    public enum Map
    {
        PLAYER,
        ENEMY
    };

    [SerializeField] private Map _targetMap;
    private TileManager _tileManager;

    private void Start()
    {
        _tileManager = TileManager.Instance;
    }

    public void Execute() {
        Vector3 destination = _targetMap == Map.PLAYER ? _tileManager.PlayerMapLastViewedPosition : _tileManager.EnemyMapLastViewedPosition;
        CameraMovement.Instance.MoveTo(destination);
    }
}
