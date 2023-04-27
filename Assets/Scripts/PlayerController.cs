using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float skiSpeed = 20f;
    [SerializeField] private float upJetForce = 7031.25f;
    [SerializeField] private float downJetForce = 5156.25f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float rotationLimit = 60f;

    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    private PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;


    void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        playerCollider.material = normalMaterial;
    }

    void OnApplicationFocus(bool hasFocus) { if (hasFocus) Cursor.lockState = CursorLockMode.Locked; }
    void OnEnable() { playerControls.Enable(); }
    void OnDisable() { playerControls.Disable(); }

    void FixedUpdate()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.MoveVector.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        bool isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f;
        bool isJumping = playerControls.Movement.Jump.ReadValue<float>() > 0.0f;
        bool isJetting = isSkiing && isJumping;
        bool isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;

        // Calculate walking/skiing movement
        playerCollider.material = isSkiing ? skiMaterial : normalMaterial;

        // Get normal of ground below player
        RaycastHit hit;
        Vector3 groundNormal = Physics.Raycast(new Ray(transform.position, Vector3.down), out hit, 3f) ? hit.normal : Vector3.up;
        // Get direction of movement relative to player rotation
        Vector3 movementDirection = transform.TransformDirection(movement).normalized;
        movementDirection = Vector3.ProjectOnPlane(movementDirection, groundNormal).normalized;
        Vector3 currentVelocity = rb.velocity;

        if (isSkiing)
        {
            // Caclulate movement vector
            Vector3 movementVector = movementDirection * skiSpeed;
            Vector3 velocityChange = movementVector - currentVelocity;

            // Calculate jetting movement
            Vector3 upJetVector = isJetting ? Vector3.up * upJetForce : Vector3.zero;
            Vector3 downJetVector = isDownJetting ? -Vector3.up * downJetForce : Vector3.zero;

            // Apply movement
            rb.AddForce(velocityChange + upJetVector + downJetVector, ForceMode.Acceleration);
        }
        else
        {
            Vector3 movementVector = movementDirection * walkSpeed;
            Vector3 velocityChange = movementVector - currentVelocity;
            rb.AddForce(velocityChange, ForceMode.Acceleration);
        }

        HandleRotation();
    }

    private void HandleRotation()
    {
        Vector2 rotationInput = playerControls.Movement.LookVector.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(0f, rotationInput.x, 0f);
        // Vector3 rotation = new Vector3(rotationInput.y, rotationInput.x, 0f);
        rotation *= rotationSpeed * Time.fixedDeltaTime;
        rotation = Vector3.ClampMagnitude(rotation, rotationLimit);
        transform.Rotate(rotation);
    }
}
