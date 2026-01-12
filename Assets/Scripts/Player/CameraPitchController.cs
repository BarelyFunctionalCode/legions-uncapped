using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPitchController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    void LateUpdate()
    {
        if (!playerController.hasFocus) return;
        Vector2 rotationInput = playerController.playerControls.Movement.Look.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(-rotationInput.y, 0f, 0f);
        rotation *= playerController.verticalRotationSpeed * Time.deltaTime;
        rotation = Vector3.ClampMagnitude(rotation, playerController.verticalRotationLimit);
        float currentXRotation = transform.eulerAngles.x < 180f ? transform.eulerAngles.x : transform.eulerAngles.x - 360f;
        rotation.x = Mathf.Clamp(currentXRotation + rotation.x, -89.0f, 89.0f) - currentXRotation;
        transform.Rotate(rotation);
    }
}
