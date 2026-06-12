using UnityEngine;
using Phase = GameManager.Phase;

public class ConeCursorController : MonoBehaviour
{
    private Renderer objectRenderer;

    [Header("Refs")]
    private TileManager _tileManager;
    private GameManager _gameManager;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        _gameManager = GameManager.Instance;
        _tileManager = TileManager.Instance;
    }

    private void Update()
    {
        UpdatePosition();
        RotateAnimation();
    }

    void UpdatePosition()
    { 
        if (_tileManager.selectedTileController != null && _gameManager.currentPhase == Phase.INIT)
        {
            objectRenderer.enabled = true;
            Vector3 selectedTilePos = _tileManager.selectedTileController.GlobalPos;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
        }
        else if (_tileManager.selectedTileController != null && _gameManager.currentPhase == Phase.PREPARATION)
        {
            objectRenderer.enabled = true;
            Vector3 selectedTilePos = _tileManager.selectedTileController.GlobalPos;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
        }
        else if (_tileManager.targetTile != null && _gameManager.currentPhase == Phase.COMMAND)
        {
            objectRenderer.enabled = true;
            Vector3 targetTilePos = _tileManager.targetTile.GlobalPos;
            transform.position = new Vector3(targetTilePos.x, transform.position.y, targetTilePos.z);
        }
        else
        {
            objectRenderer.enabled = false;
        }
    }

    void RotateAnimation()
    {
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
    }
}
