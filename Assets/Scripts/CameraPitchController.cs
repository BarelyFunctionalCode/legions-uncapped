using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPitchController : MonoBehaviour
{
    private float rotationSpeed = 40.0f;
    private float rotationLimit = 60.0f;

    private PlayerControls playerControls;

    void Awake()
    {
        playerControls = new PlayerControls();
    }

    void OnEnable() { playerControls.Enable(); }
    void OnDisable() { playerControls.Disable(); }

    void FixedUpdate()
    {
        Vector2 rotationInput = playerControls.Movement.LookVector.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(-rotationInput.y, 0f, 0f);
        rotation *= rotationSpeed * Time.fixedDeltaTime;
        rotation = Vector3.ClampMagnitude(rotation, rotationLimit);
        float currentXRotation = transform.eulerAngles.x < 180f ? transform.eulerAngles.x : transform.eulerAngles.x - 360f;
        rotation.x = Mathf.Clamp(currentXRotation + rotation.x, -89.0f, 89.0f) - currentXRotation;
        transform.Rotate(rotation);
    }
}
