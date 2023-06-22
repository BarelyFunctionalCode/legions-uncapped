using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private PlayerControls playerControls;

    [SerializeField] private float constantTimeScale = 1.0f;
    [SerializeField] private float slowDownTimeScale = 0.05f;
    private float timeScale = 1.0f;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable() { playerControls.Enable(); }
    void OnDisable() { playerControls.Disable(); }

    // void Update()
    // {
    //     bool isSlowdown = playerControls.Movement.TimeSlow.ReadValue<float>() > 0.0f;

    //     if (isSlowdown)
    //         timeScale = slowDownTimeScale;
    //     else
    //         timeScale = constantTimeScale;

    //     if (timeScale != Time.timeScale)
    //         Time.timeScale = timeScale;
    // }
}
