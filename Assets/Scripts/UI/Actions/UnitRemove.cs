using System;
using System.Threading.Tasks;
using UnityEngine;

public class UnitRemove : MonoBehaviour, IButtonAction
{
    [Header("Refs")]
    private GameManager _gameManager;
    private TileManager _tileManager;
    private UnitSpawnManager _unitSpawnManager;
    private TimelineManager _timelineManager;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        _tileManager = TileManager.Instance;
        _unitSpawnManager = UnitSpawnManager.Instance;
        _timelineManager = TimelineManager.Instance;
    }

    public void Execute() {
        try
        {
            _gameManager.IsLoading = true;
            _unitSpawnManager.DespawnUnit(_tileManager.selectedTile);
            _timelineManager.CheckPlayerTimelineCommandValidity();
        }
        catch (Exception ex)
        {
            // 通信エラーやタイムアウトのハンドリング
            Debug.LogError($"タイル更新失敗: {ex.Message}");
            // 必要に応じてユーザーに通知（ダイアログ表示など）
        }
        finally
        {
            _gameManager.IsLoading = false;
            // Debug.Log("タイル更新処理終了（後片付け完了）");
        }
    }
}
