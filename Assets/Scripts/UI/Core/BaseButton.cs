// ファイル名: ActionButton.cs
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(Button))]
public class BaseButton : MonoBehaviour {
    private Button _button;
    private IButtonAction[] _actions;
    private IButtonCondition[] _conditions;

    private void Awake() {
        _button = GetComponent<Button>();
        _actions = GetComponents<IButtonAction>();
        _conditions = GetComponents<IButtonCondition>();

        foreach (var action in _actions) {
            _button.onClick.AddListener(action.Execute);
        }
    }

    private void Update() {
        bool isInteractable = _conditions.All(c => c.CanInteract());
        
        if (_button.interactable != isInteractable) {
            _button.interactable = isInteractable;
        }
    }
}
