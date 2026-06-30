using UnityEngine;

public class TestButton : BaseButton
{
    public Vector2Int pos;

    private MapManager _mapManager;

    private void Start()
    {
        _mapManager = MapManager.Instance;
    }

    public void Onclick()
    {
        _mapManager.playerMapData[pos.x, pos.y].Stats.isSelected = true;
    }

    // public void Onclick()
    // {

    // }

    // private void CheckButtonInteractable()
    // {

    // }
}
