using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugWidgetManager : MonoBehaviour
{
    public static DebugWidgetManager Instance { get; private set; }

    [SerializeField] private GameObject debugTextWidgetPrefabObj;

    private Dictionary<string, TMP_Text> debugTextWidgets = new Dictionary<string, TMP_Text>();

    
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
    }

    public void SetDebugText(string key, string value, float xPos, float yPos)
    {
        if (!debugTextWidgets.ContainsKey(key))
        {
            GameObject debugTextWidgetObj = Instantiate(debugTextWidgetPrefabObj, transform);
            TMP_Text debugTextWidget = debugTextWidgetObj.GetComponent<TMP_Text>();
            debugTextWidget.text = "### " + key + " ###\n" + value;
            debugTextWidget.transform.SetAsFirstSibling();
            debugTextWidget.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);
            debugTextWidgets.Add(key, debugTextWidget);
        }
        else
        {
            debugTextWidgets[key].text = "### " + key + " ###\n" + value;
            debugTextWidgets[key].transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);
        }
    }

    public void RemoveDebugText(string key)
    {
        if (debugTextWidgets.ContainsKey(key))
        {
            Destroy(debugTextWidgets[key].gameObject);
            debugTextWidgets.Remove(key);
        }
    }

    public void ClearDebugText()
    {
        foreach (KeyValuePair<string, TMP_Text> debugTextWidget in debugTextWidgets)
        {
            Destroy(debugTextWidget.Value.gameObject);
        }

        debugTextWidgets.Clear();
    }
}
