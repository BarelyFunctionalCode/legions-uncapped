using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PauseMenuControl : MonoBehaviour
{
    private InputAction action;

    [SerializeField ] private new string name;
    [SerializeField ] private TMP_Text nameText;
    [SerializeField ] private TMP_Text valueText;

    [SerializeField ] private GameObject messageObj;
    [SerializeField ] private Button remapButton;

    private bool initialized = false;


    public void Initialize(InputAction action)
    {
        if (initialized)
        {
            Debug.LogError("PauseMenuControl already initialized");
            return;
        }

        messageObj.SetActive(false);
        
        this.action = action;
        this.nameText.text = action.name;
        this.valueText.text = action.GetBindingDisplayString();

        remapButton.onClick.AddListener( delegate { OnRemapButtonClicked(); } );

        initialized = true;
    }

    private void OnRemapButtonClicked()
    {
        messageObj.SetActive(true);
        valueText.gameObject.SetActive(false);

        action.PerformInteractiveRebinding()
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                UpdateValue();
            })
            .Start();
    }

    private void UpdateValue()
    {
        messageObj.SetActive(false);
        valueText.gameObject.SetActive(true);
        this.valueText.text = action.GetBindingDisplayString();
    }
}
