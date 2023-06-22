using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuOption : MonoBehaviour
{
    private PlayerController playerController;

    [SerializeField ] private new string name;
    [SerializeField ] private TMP_Text nameText;
    [SerializeField ] private TMP_Text currentValueText;
    [SerializeField ] private Slider slider;

    private bool initialized = false;


    public void Initialize(PlayerController playerController, string name, string label, float minValue, float maxValue)
    {
        if (initialized)
        {
            Debug.LogError("PauseMenuOption already initialized");
            return;
        }
        
        this.playerController = playerController;
        this.nameText.text = label;
        this.name = name;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = playerController.GetFieldValue(name);
        currentValueText.text = slider.value.ToString();

        slider.onValueChanged.AddListener(delegate { UpdateValue(); });

        initialized = true;
    }

    public void UpdateValue()
    {
        playerController.SetFieldValue(name, slider.value);
        currentValueText.text = Mathf.Round(slider.value).ToString();
    }
}
