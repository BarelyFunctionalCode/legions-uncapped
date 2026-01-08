using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PauseMenuDebug : MonoBehaviour
{
    [SerializeField] private new string name;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Toggle toggle;

    private Action<bool> updater;

    private bool initialized = false;


    public void Initialize(string name, string label, bool value, Action<bool> updater)
    {
        if (initialized)
        {
            Debug.LogError("PauseMenuOption already initialized");
            return;
        }
        
        this.nameText.text = label;
        this.name = name;
        this.updater = updater;

        toggle.isOn = value;
        toggle.onValueChanged.AddListener(delegate { UpdateValue(); });

        initialized = true;
    }

    public void UpdateValue()
    {
        this.updater(toggle.isOn);
    }
}
