using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PauseMenuOption : MonoBehaviour
{
    [SerializeField ] private TMP_Text nameText;
    [SerializeField ] private TMP_Text currentValueText;
    [SerializeField ] private Slider slider;

    private Action<float> updater;

    private bool initialized = false;


    public void Initialize(string label, float value, float minValue, float maxValue, Action<float> updater)
    {
        if (initialized)
        {
            Debug.LogError("PauseMenuOption already initialized");
            return;
        }
        
        this.nameText.text = label;
        this.updater = updater;

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        currentValueText.text = slider.value.ToString();

        slider.onValueChanged.AddListener(delegate { UpdateValue(); });

        initialized = true;
    }

    public void UpdateValue()
    {
        this.updater(slider.value);
        currentValueText.text = Mathf.Round(slider.value).ToString();
    }
}
