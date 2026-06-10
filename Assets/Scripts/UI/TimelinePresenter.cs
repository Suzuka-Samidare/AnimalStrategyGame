using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AttackCommand = AttackManager.AttackCommand;
using TileOwner = TileController.TileOwner;

public class TimelinePresenter : MonoBehaviour
{
    public static TimelinePresenter Instance { get; private set; }

    [SerializeField] private GameObject cardPrefab; // カードのプレハブ
    [SerializeField] private Transform contentParent; // ContentのTransform
    [SerializeField] private Color PlayerCommandColor;
    [SerializeField] private Color EnemyCommandColor;

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

    public void UpdateTimeline(List<AttackCommand> commands)
    {
        // 古いカードを全部消す（更新用）
        foreach (Transform child in contentParent) {
            Destroy(child.gameObject);
        }

        // Listを回してカードを生成
        foreach (var command in commands)
        {
            GameObject cardObj = Instantiate(cardPrefab, contentParent);
            // カードの中にあるテキストとかを書き換える処理をここに書く
            cardObj.GetComponent<Image>().color = command.Owner == TileOwner.Player ? PlayerCommandColor : EnemyCommandColor;
            cardObj.GetComponent<PanelView>().UpdateText(
                $"{command.UnitName} / {command.Target.gridPos} / {command.time}"
            );
        }
    }
}
