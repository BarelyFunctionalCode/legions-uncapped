using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private float timeScale = 1.0f;
    void Update()
    {
        if (timeScale != Time.timeScale)
            Time.timeScale = timeScale;
    }
}
