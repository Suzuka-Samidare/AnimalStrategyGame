using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("オブジェクト関連")]
    public TextMeshProUGUI _fpText;
    public TextMeshProUGUI _energyText;

    [Header("アニマルポイント関連")]
    [SerializeField, Tooltip("所持アニマルポイント")]
    private float _animalPoint;
    public float animalPoint
    {
        get { return _animalPoint; }
        set { _animalPoint = Mathf.Clamp(value, 0, 999); }
    }
    [SerializeField, Tooltip("アニマルポイント基礎回復値")]
    private float _apRegenValue = 1.0f;
    [SerializeField, Tooltip("アニマルポイント回復速度（秒）")]
    private float _apRegenRate = 1.5f;

    [Header("エネルギー関連")]
    [SerializeField, Tooltip("所持エネルギー")]
    private float _energy;
    public float energy
    {
        get { return _energy; }
        set { _energy = Mathf.Clamp(value, 0, 999); }
    }
    [SerializeField, Tooltip("エネルギー基礎回復値")]
    private float _energyRegenValue = 1.0f;
    [SerializeField, Tooltip("エネルギー回復速度（秒）")]
    private float _energyRegenRate = 1.5f;

    [Header("コルーチン参照用")]
    private IEnumerator _increaseEnergy;
    private IEnumerator _increaseAnimalPoint;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _increaseAnimalPoint = IncreaseStatus(_apRegenRate, _apRegenValue, AddAnimalPoint);
        _increaseEnergy = IncreaseStatus(_energyRegenRate, _energyRegenValue, AddEnergy);
    }

    void Update()
    {
        UpdateAnimalPointText();
        UpdateEnergyText();
    }

    public void AddAnimalPoint(float value)
    {
        animalPoint += value;
    }

    public void AddEnergy(float value)
    {
        energy += value;
    }

    public void UseAnimalPoint(float value)
    {
        animalPoint -= value;
    }

    public void UseEnergy(float value)
    {
        energy -= value;
    }

    public void StartRegen()
    {
        if (_increaseAnimalPoint == null || _increaseEnergy == null)
        {
            throw new Exception("AP/Energy regeneration process is missing.");
        }

        StartCoroutine(_increaseAnimalPoint);
        StartCoroutine(_increaseEnergy);
    }

    public void StopRegen()
    {
        if (_increaseAnimalPoint == null || _increaseEnergy == null)
        {
            throw new Exception("AP/Energy regeneration process is missing.");
        }

        StopCoroutine(_increaseAnimalPoint);
        StopCoroutine(_increaseEnergy);
    }

    private IEnumerator IncreaseStatus(float interval, float value, Action<float> addAction)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            addAction(value);
        }
    }

    private void UpdateAnimalPointText()
    {
        if (_fpText != null)
        {
            _fpText.text = animalPoint.ToString();
        }
        else
        {
            Debug.Log("Textパーツが参照されていません");
        }
    }

    private void UpdateEnergyText()
    {
        if (_energyText != null)
        {
            _energyText.text = energy.ToString();
        }
        else
        {
            Debug.Log("Textパーツが参照されていません");
        }
    }
}
