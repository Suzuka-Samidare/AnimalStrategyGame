using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TimelineCommand = TimelineManager.TimelineCommand;

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

    public void UpdateTimeline(List<TimelineCommand> commands)
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
            cardObj.GetComponent<Image>().color = command.Owner == Owner.Player ? PlayerCommandColor : EnemyCommandColor;
            cardObj.GetComponent<PanelView>().UpdateText(
                $"{command.UnitName}  -->  {command.TargetTile.Stats.GridPos}"
            );

            // string debugText = DumpFields(command);
            // cardObj.GetComponent<PanelView>().UpdateText(
            //     $"{debugText}"
            // );
        }
    }

    private static string DumpFields(object obj)
    {
        if (obj == null) return "null";

        Type type = obj.GetType();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"--- {type.Name} Fields ---");

        // BindingFlags で取得したいフィールドの条件を指定（public, private, インスタンス変数）
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // フィールド名と、そのインスタンスにおける現在の値を取得
            string name = field.Name;
            object value = field.GetValue(obj) ?? "null"; 
            
            sb.AppendLine($"{name}: {value}");
        }

        return sb.ToString();
    }
}
