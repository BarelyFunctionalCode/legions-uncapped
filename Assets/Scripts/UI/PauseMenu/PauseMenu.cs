using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public bool devMode { get; private set; } = true;
    [SerializeField ] private Button quitButton;
    [SerializeField ] private Button restartButton;

    [SerializeField ] private Button optionsTabButton;
    [SerializeField ] private Transform optionsListObj;
    [SerializeField ] private GameObject optionsContainerObj;
    [SerializeField ] private GameObject optionPrefabObj;
    private List<PauseMenuOption> optionsList = new List<PauseMenuOption>();

    [SerializeField ] private Button controlsTabButton;
    [SerializeField ] private Transform controlsListObj;
    [SerializeField ] private GameObject controlsContainerObj;
    [SerializeField ] private GameObject controlPrefabObj;
    private List<PauseMenuControl> controlsList = new List<PauseMenuControl>();

    [SerializeField ] private Button debugTabButton;
    [SerializeField ] private Transform debugListObj;
    [SerializeField ] private GameObject debugContainerObj;
    [SerializeField ] private GameObject debugPrefabObj;
    private List<PauseMenuDebug> debugList = new List<PauseMenuDebug>();


    private void Awake() 
    { 
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        } 

        quitButton.onClick.AddListener( delegate { OnQuitButtonClicked(); } );
        restartButton.onClick.AddListener( delegate { OnRestartButtonClicked(); } );
        optionsTabButton.onClick.AddListener( delegate { OnOptionsTabButtonClicked(); } );
        controlsTabButton.onClick.AddListener( delegate { OnControlsTabButtonClicked(); } );
        debugTabButton.onClick.AddListener( delegate { OnDebugTabButtonClicked(); } );
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(false);
        gameObject.SetActive(false);

        debugTabButton.gameObject.SetActive(devMode);
    }

    private void OnQuitButtonClicked() { Application.Quit(); }

    private void OnRestartButtonClicked() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    private void OnOptionsTabButtonClicked()
    {
        optionsContainerObj.SetActive(true);
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(false);
    }

    private void OnControlsTabButtonClicked()
    {
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(true);
        debugContainerObj.SetActive(false);
    }

    private void OnDebugTabButtonClicked()
    {
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(true);
    }

    public void AddOption(string label, float value, float minValue, float maxValue, Action<float> updater)
    {
        GameObject optionObj = Instantiate(optionPrefabObj, optionsListObj);
        optionObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -optionsList.Count * optionObj.GetComponent<RectTransform>().rect.height);
        PauseMenuOption option = optionObj.GetComponent<PauseMenuOption>();
        option.Initialize(label, value, minValue, maxValue, updater);
        optionsList.Add(option);
    }

    public void AddControl(InputAction action)
    {
        GameObject controlObj = Instantiate(controlPrefabObj, controlsListObj);
        controlObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -controlsList.Count * controlObj.GetComponent<RectTransform>().rect.height);
        PauseMenuControl control = controlObj.GetComponent<PauseMenuControl>();
        control.Initialize(action);
        controlsList.Add(control);
    }

    public void AddDebug(string name, string label, bool value, Action<bool> updater)
    {
        GameObject debugObj = Instantiate(debugPrefabObj, debugListObj);
        debugObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -debugList.Count * debugObj.GetComponent<RectTransform>().rect.height);
        PauseMenuDebug debug = debugObj.GetComponent<PauseMenuDebug>();
        debug.Initialize(name, label, value, updater);
        debugList.Add(debug);
    }
}
