using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTelemetry
{
    [PauseMenuDevOption("Surface Data")]
    public bool enableSurfaceDebug = false;

    [PauseMenuDevOption("Movement Data")]
    public bool enableMovementDebug = false;


    private DevVectorRenderer devVectorRenderer;

    public Vector3 position;
    public Vector3 velocity;

    public Vector3 movementDirection;

    public Vector3 surfaceNormal;
    public Vector3 surfacePoint;
    public float distanceToSurface;
    
    public bool isSkiing;
    public bool isUpJetting;
    public bool isDownJetting;
    public bool isGrounded;

    public PlayerTelemetry(DevVectorRenderer devVectorRenderer)
    {
        this.devVectorRenderer = devVectorRenderer;
        this.position = Vector3.zero;
        this.velocity = Vector3.zero;
        this.movementDirection = Vector3.zero;
        this.surfaceNormal = Vector3.zero;
        this.surfacePoint = Vector3.zero;
        this.distanceToSurface = 0.0f;
        this.isSkiing = false;
        this.isUpJetting = false;
        this.isDownJetting = false;
        this.isGrounded = false;
    }

    public void Update()
    {
        if (enableSurfaceDebug)
        {
            devVectorRenderer.AddDevVector(surfacePoint, surfaceNormal * 0.5f, new Color(0f, 1f, 0f, 0.2f), 5.0f, 0.1f);

            DebugWidgetManager.Instance.SetDebugText("Terrain Surface",
            $"Point: {surfacePoint:F2}\nNormal: {surfaceNormal:F2}\nDistance: {distanceToSurface:F2}",
            100, -200);
        }
        else
        {
            DebugWidgetManager.Instance.RemoveDebugText("Terrain Surface");
        }

        if (enableMovementDebug)
        {
            devVectorRenderer.AddDevVector(position, velocity.normalized, Color.blue, 5.0f);

            DebugWidgetManager.Instance.SetDebugText("Velocity",
            $"Speed: {velocity.magnitude:F2}\nDirection: {velocity.normalized:F0}\nDesired Direction: {movementDirection:F0}\nIs Grounded: {isGrounded}\nIs Skiing: {isSkiing}\nIs Up Jetting: {isUpJetting}\nIs Down Jetting: {isDownJetting}",
            100, -300);
        }
        else
        {
            DebugWidgetManager.Instance.RemoveDebugText("Velocity");
        }
    }
}


public class PlayerController : Entity
{
    [SerializeField] private DevVectorRenderer devVectorRenderer;
    [SerializeField] private HUD hud;

    public PlayerTelemetry playerTelemetry;

    private readonly float drag = 0.004f;                        
    private readonly float airCushionDrag = 0.00275f;             
    private readonly float airCushionHeight = 10f;    
    private readonly float mass = 75f;

    [PauseMenuOption("Horizontal Look", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;
    private readonly float horizontalRotationLimit = 100f;

    [PauseMenuOption("Vertical Look", 0f, 100f)]
    public float verticalRotationSpeed = 60f;
    public float verticalRotationLimit = 100f;

    [Header("Physics")]
    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    public PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;


    [Header("Hovering")]
    [Range(0.0f, 2.0f)]
    [SerializeField] private float hoverHeightMax = 0.02f;

    [Header("Skiing")]
    [PauseMenuDevOption("Ski Force", 0f, 300f)]
    [SerializeField] public float skiStrength = 30f;

    [PauseMenuDevOption("Skiing Resist Speed", 0f, 300f)]
    [SerializeField] public float resistSkiSpeed = 20f;

    [PauseMenuDevOption("Skiing Max Speed", 0f, 300f)]
    [SerializeField] public float maxSkiSpeed = 40f;


    [Header("Jetting")]
    [PauseMenuDevOption("Up Jet Force", 0f, 300f)]
    [SerializeField] public float upJetStrength = 40f;

    [PauseMenuDevOption("Up Jeting Resist Speed", 0f, 300f)]
    [SerializeField] public float resistUpJetSpeed = 60f;

    [PauseMenuDevOption("Up Jeting Max Speed", 0f, 300f)]
    [SerializeField] public float maxUpJetSpeed = 120f;

    
    [PauseMenuDevOption("Down Jet Force", 0f, 300f)]
    [SerializeField] public float downJetStrength = 40f;

    [PauseMenuDevOption("Down Jeting Resist Speed", 0f, 300f)]
    [SerializeField] public float resistDownJetSpeed = 60f;

    [PauseMenuDevOption("Down Jeting Max Speed", 0f, 300f)]
    [SerializeField] public float maxDownJetSpeed = 120f;

    [PauseMenuDevOption("Jetting Energy Drain", 0f, 100f)]
    [SerializeField] public float jettingEnergyDrain = 20f;

    [Header("Running")]
    [PauseMenuDevOption("Run Force", 0f, 50f)]
    [SerializeField] public float runStrength = 40f;

    [PauseMenuDevOption("Running Resist Speed", 0f, 50f)]
    [SerializeField] public float resistRunSpeed = 18f;

    [PauseMenuDevOption("Running Max Speed", 0f, 50f)]
    [SerializeField] public float maxRunSpeed = 20f;

    [Header("Jumping")]
    [PauseMenuDevOption("Jump Force", 0f, 3000f)]
    [SerializeField] public float jumpStrength = 2200f;

    [Header("Air Control")]
    [PauseMenuDevOption("Air Control Force", 0f, 10f)]
    [SerializeField] public float airControlStrength = 2f;

    [PauseMenuDevOption("Air Control Resist Speed", 0f, 50f)]
    [SerializeField] public float resistAirControlSpeed = 18f;

    [PauseMenuDevOption("Air Control Max Speed", 0f, 50f)]
    [SerializeField] public float maxAirControlSpeed = 20f;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask ignoreLayers;


    bool skiToggleInput = false;
    bool skiToggle = false;

    Vector3 movementDirection = Vector3.zero;
    bool isJumping = false;
    bool isSkiing = false;
    bool isUpJetting = false;
    bool isDownJetting = false;
    bool isJetting = false;
    bool isMoving = false;
    bool isRunning = false;
    bool isGrounded = false;

    Vector3 surfaceNormal = Vector3.up;
    Vector3 surfacePoint = Vector3.zero;
    float distanceToSurface = Mathf.Infinity;

    public bool hasFocus = false;

    [SerializeField] private Transform playerUI;

    [SerializeField] private Transform weaponMountPoint;
    [SerializeField] private Transform throwableMountPoint;

    private PlayerLoadout playerLoadout;

    protected override void Awake()
    {
        base.Awake();

        playerTelemetry = new PlayerTelemetry(devVectorRenderer);

        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.0f;
        rb.mass = mass;
        playerCollider = GetComponent<CapsuleCollider>();
        playerCollider.material = normalMaterial;
        animator = GetComponent<Animator>();
        playerLoadout = GetComponent<PlayerLoadout>();
        playerLoadout.Initialize(this, weaponMountPoint, throwableMountPoint);
        hud.Initialize(this);
    }

    void Start()
    {
        InitializePauseMenuElements();
    }

    void OnApplicationFocus(bool tempHasFocus)
    {
        if (tempHasFocus) Cursor.lockState = CursorLockMode.Locked;
        hasFocus = tempHasFocus;
    }
    void OnEnable()
    {
        playerControls.Enable();
        playerControls.Equipment.NextWeapon.started += playerLoadout.NextWeapon;
        playerControls.Equipment.PreviousWeapon.started += playerLoadout.PreviousWeapon;
        playerControls.Movement.JumpJet.started += OnJumpStarted;
    }
    void OnDisable()
    {
        playerControls.Disable();
        playerControls.Equipment.NextWeapon.started -= playerLoadout.NextWeapon;
        playerControls.Equipment.PreviousWeapon.started -= playerLoadout.PreviousWeapon;
        playerControls.Movement.JumpJet.started -= OnJumpStarted;
    }

    private void OnJumpStarted(InputAction.CallbackContext _)
    {
        isJumping = true;
    }

    protected override void Update()
    {
        base.Update();

        HandleCollision();
        HandleInputs();

        bool tempSkiToggleInput = playerControls.Movement.ToggleSki.ReadValue<float>() > 0.0f;
        if (tempSkiToggleInput && !skiToggleInput)
        {
            skiToggle = !skiToggle;
        }
        skiToggleInput = tempSkiToggleInput;

        if (playerControls.UI.Pause.WasPressedThisFrame())
        {
            bool newMenuState = !playerUI.Find("PauseMenu").gameObject.activeSelf;
            Cursor.lockState = newMenuState ? CursorLockMode.Confined : CursorLockMode.Locked;
            Time.timeScale = newMenuState ? 0.0f : 1.0f;
            if (newMenuState) {
                playerControls.Disable();
                playerControls.UI.Enable();
            }
            else playerControls.Enable();
            playerUI.Find("PauseMenu").gameObject.SetActive(newMenuState);
        }

        playerTelemetry.Update();

        // Set friction based on whether player is skiing or not
        playerCollider.material = isSkiing ? skiMaterial : normalMaterial;

        // Apply Drag
        rb.drag = distanceToSurface <= airCushionHeight ? airCushionDrag : drag;
    }

    void FixedUpdate()
    {
        HandleMovement();

        if (hasFocus) HandleRotation();
        playerTelemetry.position = transform.position;
        playerTelemetry.velocity = rb.velocity;
    }

    private void HandleInputs()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.Move.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);

        // Get direction of movement relative to player rotation
        movementDirection = transform.TransformDirection(movement).normalized;
    
        // Get input for skiing, jumping, and down jetting
        isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f || skiToggle;
        isUpJetting = isSkiing && playerControls.Movement.JumpJet.ReadValue<float>() > 0.0f;
        isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;
        isJetting = isUpJetting || isDownJetting;
        isMoving = movement.magnitude > 0.0f;

        isRunning = isGrounded && isMoving && !isSkiing;
        
        playerTelemetry.movementDirection = movementDirection;
        playerTelemetry.isSkiing = isSkiing;
        playerTelemetry.isUpJetting = isUpJetting;
        playerTelemetry.isDownJetting = isDownJetting;

        // Set animator values
        Vector3 animMovementDirectionNewY = Vector3.up * (isDownJetting ? -1f : (isUpJetting ? 1f : 0f));
        animMovementDirection = Vector3.Lerp(animMovementDirection, movement.normalized + animMovementDirectionNewY, Time.fixedDeltaTime * 10f);
        animator.SetFloat("xDir", animMovementDirection.x);
        animator.SetFloat("yDir", animMovementDirection.y);
        animator.SetFloat("zDir", animMovementDirection.z);
        animator.SetFloat("yVel", rb.velocity.normalized.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isSkiing", isSkiing && !isUpJetting && !isDownJetting);
        animator.SetBool("isJetting", isUpJetting || isDownJetting);
    }

    private void HandleCollision()
    {
        isGrounded = false;
        distanceToSurface = Mathf.Infinity;

        // Raycast to last known ground location
        Vector3 groundCheckPoint = rb.position + (Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized * 0.5f) + (Vector3.up * playerCollider.bounds.extents.y);
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                groundCheckPoint,
                Vector3.down
            ),
            out hit,
            Mathf.Infinity,
            ~ignoreLayers
        );
        if (didHit)
        {
            surfaceNormal = hit.normal;
            surfacePoint = hit.point;

            // Artificially increase distance to surface by 0.5f to prevent player from being grounded when they shouldn't be
            distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, groundCheckPoint - (Vector3.up * playerCollider.bounds.extents.y * 2.0f)) - 0.5f, 0.0f);
        }

        if (distanceToSurface < hoverHeightMax * 2.0f)
        {
            isGrounded = true;
        }

        playerTelemetry.isGrounded = isGrounded;
        playerTelemetry.surfacePoint = surfacePoint;
        playerTelemetry.surfaceNormal = surfaceNormal;
        playerTelemetry.distanceToSurface = distanceToSurface;
    }


    private void HandleMovement()
    {
        // Skiing Movement
        if (isSkiing && GetEnergy() > 0.1f)
        {
            // Hovering
            // More force the closer to the surface...
            float hoverFactor = Mathf.Clamp01(1.0f - (distanceToSurface - hoverHeightMax)/hoverHeightMax) * 1.1f;

            Vector3 lateralVelocityDir = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized;
            float surfaceNormalDotLateralVelocityDirection = Vector3.Dot(surfaceNormal, lateralVelocityDir);

            Vector3 acc = Vector3.zero;
            // Going Downhill?
            if (surfaceNormalDotLateralVelocityDirection > 0.0f)
            {
                // player is pushed fast downhill... easy
                acc.x = hoverFactor * surfaceNormal.x * Physics.gravity.magnitude * 2.0f;
                acc.z = hoverFactor * surfaceNormal.z * Physics.gravity.magnitude * 2.0f;
            }
            // Going Uphill?
            else
            {
                // surfaceNormalDotLateralVelocityDirection is negative
                // lateralVelocityDir is is obviously pointing into the surface
                // flip lateralVelocityDir and scale it depending on how steep the surface is

                // On steep surfaces, if player is flying into the surface it's nearly 0,
                // If going up the surface, its slightly uphill of the surface

                // if the surface is not steep, the player is slightly pushed uphill
                Vector3 adjustedSurfaceNormal = surfaceNormal - lateralVelocityDir * surfaceNormalDotLateralVelocityDirection;

                Vector3 lateralVelocityDirOrUp = lateralVelocityDir.magnitude > 0.0f ? lateralVelocityDir : Vector3.up;

                // pointing away from surface
                Vector3 reverseLateralVelocityDir = -lateralVelocityDirOrUp;

                // this would be positive, right?
                float adjustedSurfaceNormalDotReverseLateralVelocityDir = Vector3.Dot(adjustedSurfaceNormal, reverseLateralVelocityDir);

                // player is pushed away and uphill of surface, seems like it wouldn't work?
                acc.x = (adjustedSurfaceNormal.x + lateralVelocityDir.x * adjustedSurfaceNormalDotReverseLateralVelocityDir) * hoverFactor * Physics.gravity.magnitude * 0.5f;
                acc.z = (adjustedSurfaceNormal.z + lateralVelocityDir.z * adjustedSurfaceNormalDotReverseLateralVelocityDir) * hoverFactor * Physics.gravity.magnitude * 0.5f;
            }
            acc.y = hoverFactor * Physics.gravity.magnitude;
            rb.AddForce(acc, ForceMode.Acceleration);


            // Jetting Force
            Vector3 jetForce = Vector3.zero;

            // Horizontal Jetting Force
            float currentLateralSpeed = Vector3.Dot(rb.velocity, movementDirection);
            float skiResistMultiplier = Mathf.Max(1.0f - ((currentLateralSpeed - resistSkiSpeed) / (maxSkiSpeed - resistSkiSpeed)), 0.0f);
            jetForce += movementDirection * skiStrength * skiResistMultiplier;

            // Vertical Jetting Force
            float currentUpSpeed = Vector3.Dot(rb.velocity, Vector3.up);
            float verticalResistSpeed = currentUpSpeed > 0.0f ? resistUpJetSpeed : resistDownJetSpeed;
            float verticalMaxSpeed = currentUpSpeed > 0.0f ? maxUpJetSpeed : maxDownJetSpeed;
            float verticalJetResistMultiplier = Mathf.Max(1.0f - ((currentUpSpeed - verticalResistSpeed) / (verticalMaxSpeed - verticalResistSpeed)), 0.0f);
            if (isUpJetting) jetForce += Vector3.up * upJetStrength * verticalJetResistMultiplier;
            if (isDownJetting) jetForce += -Vector3.up * downJetStrength * verticalJetResistMultiplier;

            if (isJetting) ApplyEnergyDelta(-jettingEnergyDrain * Time.fixedDeltaTime);

            rb.AddForce(jetForce, ForceMode.Acceleration);
        }

        // Running Movement
        if (isRunning)
        {
            float currentSpeed = Vector3.Dot(rb.velocity, movementDirection);
            float resistMultiplier = Mathf.Max(1.0f - ((currentSpeed - resistRunSpeed) / (maxRunSpeed - resistRunSpeed)), 0.0f);
            Vector3 force = movementDirection * runStrength * resistMultiplier;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        // Jumping
        if (isJumping && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
            animator.SetTrigger("triggerJump");
            isJumping = false;
        }

        // Air Control
        if (!isGrounded && !isSkiing)
        {
            float currentSpeed = Vector3.Dot(rb.velocity, movementDirection);
            float resistMultiplier = Mathf.Max(1.0f - ((currentSpeed - resistAirControlSpeed) / (maxAirControlSpeed - resistAirControlSpeed)), 0.0f);
            Vector3 force = movementDirection * airControlStrength * resistMultiplier;
            rb.AddForce(force, ForceMode.Acceleration);
        }
    }

    private void HandleRotation()
    {
        Vector2 rotationInput = playerControls.Movement.Look.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(0f, rotationInput.x, 0f);
        rotation *= horizontalRotationSpeed * Time.fixedDeltaTime;
        rotation = Vector3.ClampMagnitude(rotation, horizontalRotationLimit);

        Quaternion newRot = Quaternion.Euler(rb.rotation.eulerAngles + rotation);

        rb.MoveRotation(newRot);
    }

    private void InitializePauseMenuElements()
    {
        // Initialize player options in pause menu
        FieldInfo[] fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

            if (attribute.Length > 0)
            {
                if (!PauseMenu.Instance.devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                PauseMenu.Instance.AddOption(
                    attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
                    (float)field.GetValue(this),
                    attribute[0].minValue,
                    attribute[0].maxValue,
                    (float value) => { field.SetValue(this, value); }
                );
            }
        }

        // Initialize player controls in pause menu
        List<string> controlIgnoreList = new List<string> { "Pause","Move", "Look" };
        // InputActionMap movementMap = playerControls.Movement;
        foreach (var actionMap in playerControls.asset.actionMaps)
        {
            foreach (var action in actionMap)
            {
                if (controlIgnoreList.Contains(action.name)) continue;
                PauseMenu.Instance.AddControl(action);
            }
        }

        // Initialize player debug settings in pause menu
        if (!PauseMenu.Instance.devMode) return;
        fields = playerTelemetry.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                PauseMenu.Instance.AddDebug(
                    field.Name,
                    attribute[0].label,
                    (bool)field.GetValue(playerTelemetry),
                    (bool value) => { field.SetValue(playerTelemetry, value); }
                );
            }
        }
    }

    protected override void OnDie()
    {
        print("Player Died");
    }
}
