using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    private bool devMode = true;
    [SerializeField ] private PlayerController playerController;
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
    private List<string> controlIgnoreList = new List<string> { "Move", "Look" };


    private void Awake()
    {
        quitButton.onClick.AddListener( delegate { OnQuitButtonClicked(); } );
        restartButton.onClick.AddListener( delegate { OnRestartButtonClicked(); } );
        optionsTabButton.onClick.AddListener( delegate { OnOptionsTabButtonClicked(); } );
        controlsTabButton.onClick.AddListener( delegate { OnControlsTabButtonClicked(); } );
        controlsContainerObj.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Start()
    {
        FieldInfo[] fields = playerController.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);


            if (attribute.Length > 0)
            {
                if (!devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                AddOption(
                    field.Name,
                    attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
                    attribute[0].minValue,
                    attribute[0].maxValue
                );
            }
        }

        InputActionMap movementMap = playerController.playerControls.Movement;
        foreach (var action in movementMap)
        {
            if (controlIgnoreList.Contains(action.name)) continue;
            AddControl(action);
        }
    }

    void OnEnable() { if (playerController?.playerControls != null) playerController.playerControls.Movement.Disable(); }
    void OnDisable() { if (playerController?.playerControls != null) playerController.playerControls.Movement.Enable(); }

    private void OnQuitButtonClicked() { Application.Quit(); }

    private void OnRestartButtonClicked() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    private void OnOptionsTabButtonClicked()
    {
        optionsContainerObj.SetActive(true);
        controlsContainerObj.SetActive(false);
    }

    private void OnControlsTabButtonClicked()
    {
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(true);
    }


    private void AddOption(string name, string label, float minValue, float maxValue)
    {
        GameObject optionObj = Instantiate(optionPrefabObj, optionsListObj);
        optionObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -optionsList.Count * optionObj.GetComponent<RectTransform>().rect.height);
        PauseMenuOption option = optionObj.GetComponent<PauseMenuOption>();
        option.Initialize(playerController, name, label, minValue, maxValue);
        optionsList.Add(option);
    }

    private void AddControl(InputAction action)
    {
        GameObject controlObj = Instantiate(controlPrefabObj, controlsListObj);
        controlObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -controlsList.Count * controlObj.GetComponent<RectTransform>().rect.height);
        PauseMenuControl control = controlObj.GetComponent<PauseMenuControl>();
        control.Initialize(playerController, action);
        controlsList.Add(control);
    }
}
