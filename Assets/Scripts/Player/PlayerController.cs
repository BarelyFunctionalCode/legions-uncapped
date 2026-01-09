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
    public bool previousIsGrounded;

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
            $"Speed: {velocity.magnitude:F2}\nDirection: {velocity.normalized:F1}\nDesired Direction: {movementDirection:F1}\nIs Grounded: {isGrounded}\nIs Skiing: {isSkiing}\nIs Up Jetting: {isUpJetting}\nIs Down Jetting: {isDownJetting}",
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
    public float verticalRotationSpeed = 24f;
    public float verticalRotationLimit = 100f;

    [Header("Physics")]
    [SerializeField] private PhysicsMaterial skiMaterial;
    [SerializeField] private PhysicsMaterial normalMaterial;

    public PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;


    [Header("Hovering")]
    [Range(0.0f, 2.0f)]
    [SerializeField] private float hoverHeightMax = 0.02f;

    [Header("Jetting")]
    [PauseMenuDevOption("Up Jet Force", 0f, 10000f)]
    [SerializeField] public float upJetForce = 7656.25f;

    [PauseMenuDevOption("Down Jet Force", 0f, 10000f)]
    [SerializeField] public float downJetForce = 5625f;
    [PauseMenuDevOption("Jet Air Move Min Speed", 0f, 100f)]
    [SerializeField] public float jetAirMoveMinSpeed = 5f;
    [PauseMenuDevOption("Jet Air Move Max Speed", 0f, 2000f)]
    [SerializeField] public float jetAirMoveMaxSpeed = 1000f;
    [PauseMenuDevOption("Jet Air Move Max Accel Factor", 0f, 5f)]
    [SerializeField] public float jetAirMoveMaxAccelFactor = 1.5f;
    [PauseMenuDevOption("Jet Directional Force XY", 0f, 10000f)]
    [SerializeField] public float jetDirectionalForceXY = 3125f;

    [PauseMenuDevOption("Up Jetting Energy Drain", 0f, 100f)]
    [SerializeField] public float upJettingEnergyDrain = 22.5f;

    [PauseMenuDevOption("Down Jetting Energy Drain", 0f, 100f)]
    [SerializeField] public float downJettingEnergyDrain = 21.875f;

    [PauseMenuDevOption("Jet Skating Energy Drain", 0f, 100f)]
    [SerializeField] public float jetSkateEnergyDrain = 4f;

    [Header("Running")]
    [PauseMenuDevOption("Run Force", 0f, 20000f)]
    [SerializeField] public float runForce = 10000f;

    [PauseMenuDevOption("Running Max Speed", 0f, 50f)]
    [SerializeField] public float maxRunSpeed = 15f;

    [Header("Jumping")]
    [PauseMenuDevOption("Jump Force", 0f, 3000f)]
    [SerializeField] public float jumpForce = 2000f;

    [PauseMenuDevOption("Min Jump Speed", 0f, 50f)]
    [SerializeField] public float minJumpSpeed = 10f;

    [PauseMenuDevOption("Max Jump Speed", 0f, 50f)]
    [SerializeField] public float maxJumpSpeed = 15f;

    [PauseMenuDevOption("Jump Surface Angle", 0f, 90f)]
    [SerializeField] public float jumpSurfaceAngle = 80f;

    [Header("Air Control")]
    [PauseMenuDevOption("Air Control Factor", 0f, 1f)]
    [SerializeField] public float airControl = 1f;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask ignoreLayers;

    float horizontalJetResistance = 0.0017f;
    float horizontalJetResistanceFactor = 1.8f;
    float verticalJetResistance = 0.0006f;
    float verticalJetResistanceFactor = 1.8f;

    float horizResistFactor = 0.3f;
    float horizResistSpeed = 100f;
    float horizMaxSpeed = 120f;
    float upResistFactor = 0.3f;
    float upResistSpeed = 85f;
    float upMaxSpeed = 115f;
    float downResistFactor = 0.1f;
    float downResistSpeed = 1000f;
    float downMaxSpeed = 1000f;

    bool skiToggleInput = false;
    bool skiToggle = false;

    Vector3 movementInput = Vector3.zero;
    Vector3 movementDirection = Vector3.zero;
    bool isJumping = false;
    bool isSkiing = false;
    bool isUpJetting = false;
    bool isDownJetting = false;
    bool isJetting = false;
    bool isMoving = false;
    bool isRunning = false;
    int lastGroundedFrames = 0;

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
        rb.linearDamping = distanceToSurface <= airCushionHeight ? airCushionDrag : drag;
    }

    void FixedUpdate()
    {
        HandleMovement();
        isJumping = false;

        if (hasFocus) HandleRotation();
        playerTelemetry.position = transform.position;
        playerTelemetry.velocity = rb.linearVelocity;
    }

    private void HandleInputs()
    {
        // Get movement input
        Vector2 movement = playerControls.Movement.Move.ReadValue<Vector2>();
        movementInput = new Vector3(movement.x, 0f, movement.y);

        // Get direction of movement relative to player rotation
        movementDirection = transform.TransformDirection(movementInput); // NOT SUPPOSED TO BE NORMALIZED
    
        // Get input for skiing, jumping, and down jetting
        isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f || skiToggle;
        isUpJetting = isSkiing && playerControls.Movement.JumpJet.ReadValue<float>() > 0.0f;
        isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;
        isJetting = isUpJetting || isDownJetting;
        isMoving = movementInput.magnitude > 0.0f;

        isRunning = isGrounded && isMoving && !isSkiing;
        
        playerTelemetry.movementDirection = movementDirection;
        playerTelemetry.isSkiing = isSkiing;
        playerTelemetry.isUpJetting = isUpJetting;
        playerTelemetry.isDownJetting = isDownJetting;

        // Set animator values
        Vector3 animMovementDirectionNewY = Vector3.up * (isDownJetting ? -1f : (isUpJetting ? 1f : 0f));
        animMovementDirection = Vector3.Lerp(animMovementDirection, movementInput.normalized + animMovementDirectionNewY, Time.fixedDeltaTime * 10f);
        animator.SetFloat("xDir", animMovementDirection.x);
        animator.SetFloat("yDir", animMovementDirection.y);
        animator.SetFloat("zDir", animMovementDirection.z);
        animator.SetFloat("yVel", rb.linearVelocity.normalized.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isSkiing", isSkiing && !isUpJetting && !isDownJetting);
        animator.SetBool("isJetting", isUpJetting || isDownJetting);
    }

    private void HandleCollision()
    {
        lastGroundedFrames++;
        isGrounded = false;
        distanceToSurface = Mathf.Infinity;
        surfaceNormal = Vector3.up;
        surfacePoint = Vector3.zero;

        // Raycast to last known ground location
        Vector3 groundCheckPoint = rb.position;
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                groundCheckPoint,
                Vector3.down
            ),
            out hit,
            distanceToSurface,
            ~ignoreLayers
        );
        if (didHit)
        {
            playerTelemetry.isGrounded = isGrounded;
            
            // Surface too steep
            float slope = Vector3.Dot(hit.normal, Vector3.up);
            if (slope < 0.5f) return;

            surfacePoint = hit.point;
            distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, groundCheckPoint) - playerCollider.bounds.extents.y, 0.0f);
            playerTelemetry.distanceToSurface = distanceToSurface;
            playerTelemetry.surfacePoint = surfacePoint;

            // Breakaway vertical speed check
            if (rb.linearVelocity.y > 2.0f) return;

            if (distanceToSurface <= 0.15f)
            {
                isGrounded = true;
                lastGroundedFrames = 0;
            }
            else if (lastGroundedFrames < 8) // TODO: This should changed to a timer-based check rather than frame-based since it can vary with framerate
            {
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }

            if (isGrounded)
            {
                surfaceNormal = hit.normal;
            }

        }
        playerTelemetry.isGrounded = isGrounded;
        playerTelemetry.surfaceNormal = surfaceNormal;
        
    }


    private void HandleMovement()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 desiredAcc = Vector3.zero;
        Vector3 groundImpulse = Vector3.zero;
        float desiredVerticalAcc = 0f;

        // Air Control
        if (!isGrounded && !isJetting && !isSkiing)
        {
            Vector3 airDirection = movementDirection.normalized;
            Vector3 airControlAcc = airDirection * airControl;

            float maxAccel = runForce / mass * Time.fixedDeltaTime * 0.3f;

            if (airControlAcc.magnitude > maxAccel)
            {
                airControlAcc = airControlAcc.normalized * maxAccel;
            }
            desiredAcc.x += airControlAcc.x;
            desiredAcc.z += airControlAcc.z;
        }

        // Running Movement
        if (isRunning)
        {
            float slopeDot = Vector3.Dot(currentVelocity, surfaceNormal);

            if (slopeDot < 0.0f)
                groundImpulse -= surfaceNormal * (slopeDot + 0.002f);

            Vector3 runDirection = movementDirection.normalized;
            Vector3 sideDirection = Vector3.Cross(runDirection, Vector3.up).normalized;
            Vector3 forwardDirection = (runDirection - sideDirection * Vector3.Dot(runDirection, sideDirection)).normalized;

            Vector3 targetVelocity = forwardDirection * maxRunSpeed;
            Vector3 velocityDiff = targetVelocity - (currentVelocity + groundImpulse);

            float maxRunAccel = runForce / mass * Time.fixedDeltaTime;
            groundImpulse += velocityDiff.normalized * Mathf.Min(velocityDiff.magnitude, maxRunAccel);
        }

        // Jumping
        if (isJumping && isGrounded && currentVelocity.y <= maxJumpSpeed)
        {
            float jumpScale = 1.0f;

            if (currentVelocity.y < minJumpSpeed)
            {
                jumpScale = 1.0f - (currentVelocity.y / minJumpSpeed) / (maxJumpSpeed / minJumpSpeed);
            }

            Vector3 jumpDirection = movementDirection.normalized;

            float playerScaleFactor = transform.localScale.y * 0.25f + 0.75f;
            float jumpForceFinal = jumpForce / rb.mass;

            float surfaceNormalDotJumpDirection = Vector3.Dot(jumpDirection, surfaceNormal);

            if (surfaceNormalDotJumpDirection > 0.0f)
            {
                desiredAcc.x += surfaceNormal.x * playerScaleFactor * jumpForceFinal;
                desiredAcc.z += surfaceNormal.z * playerScaleFactor * jumpForceFinal;
            }

            Vector3 jumpSurfaceNormal = Vector3.Angle(surfaceNormal, Vector3.up) <= jumpSurfaceAngle ?
                surfaceNormal :
                Vector3.zero;
            desiredVerticalAcc = jumpSurfaceNormal.y * playerScaleFactor * jumpForceFinal * jumpScale;
            lastGroundedFrames = 8;
        }

        // Skiing Movement
        if (isSkiing && GetEnergy() > 0.0f)
        {
            // Hovering
            // More force the closer to the surface...
            // distanceToSurface -= 0.5f; // Artificially decrease distance to surface to make hovering easier
            float hoverFactor = Mathf.Clamp01(1.0f - (distanceToSurface - hoverHeightMax) / hoverHeightMax) * 1.1f;

            Vector3 lateralVelocityDir = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).normalized;
            float surfaceNormalDotLateralVelocityDirection = Vector3.Dot(surfaceNormal, lateralVelocityDir);

            if (surfaceNormalDotLateralVelocityDirection > 0.0f)
            {
                // Going Downhill?
                // player is pushed fast downhill... easy
                desiredAcc = 2.0f * hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);
            }
            else
            {
                // Going Uphill?
                Vector3 surfaceDirection = (surfaceNormal - lateralVelocityDir * surfaceNormalDotLateralVelocityDirection).normalized;
                Vector3 sideDirection = -lateralVelocityDir;
                float sideDot = Vector3.Dot(surfaceDirection, sideDirection);
                
                desiredAcc = 0.5f * hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * (surfaceDirection - lateralVelocityDir * sideDot);
            }
            desiredAcc.y = 0.0f;
            Vector3 hoverVertAcc = hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * Vector3.up;
            rb.AddForce(hoverVertAcc, ForceMode.VelocityChange);
            currentVelocity += hoverVertAcc;
        }

        // Jetting Movement
        float speed = currentVelocity.magnitude;
        if (speed < jetAirMoveMaxSpeed)
        {
            float accelScale = 1.0f;
            if (speed < jetAirMoveMinSpeed && speed > 0.01f)
            {
                accelScale = Mathf.Min(
                    jetAirMoveMinSpeed / speed,
                    jetAirMoveMaxAccelFactor);
            }

            // Directional Control while Jetting/Skiing
            if (isSkiing && movementDirection.magnitude > 0.01f && GetEnergy() > 0.0f)
            {
                float lateralForce = jetDirectionalForceXY / mass * accelScale * Time.fixedDeltaTime;
                desiredAcc += movementDirection * lateralForce;
                ApplyEnergyDelta(-jetSkateEnergyDrain * Time.fixedDeltaTime);
            }

            // Up Jetting
            if (isJetting && GetEnergy() > 0.001f)
            {
                float force = 0f;
                if (isUpJetting)
                {
                    float cushion = 1.0f;
                    if (distanceToSurface <= airCushionHeight)
                        cushion = (airCushionHeight - distanceToSurface) / airCushionHeight;

                    force = upJetForce / mass * accelScale * Time.fixedDeltaTime;
                    force += force * cushion * 0.5f;
                }
                else if (isDownJetting) // Down Jetting
                {
                    force = -downJetForce / mass * accelScale * Time.fixedDeltaTime;
                }

                desiredVerticalAcc = force;
                ApplyEnergyDelta(- (isUpJetting ? upJettingEnergyDrain : downJettingEnergyDrain) * Time.fixedDeltaTime);
            }
        }


        // Apply Jet Resistance
        Vector3 jetResistance = CalculateJetResistance(currentVelocity, desiredVerticalAcc, desiredAcc);
        rb.AddForce(jetResistance, ForceMode.VelocityChange);
        currentVelocity += jetResistance;


        // Apply desired acceleration, jetting accelration, and gravity
        desiredAcc.y += desiredVerticalAcc;
        desiredAcc.y -= Physics.gravity.magnitude * Time.fixedDeltaTime;
        desiredAcc += groundImpulse;
        rb.AddForce(desiredAcc, ForceMode.VelocityChange);
        currentVelocity += desiredAcc;

        // Apply velocity caps
        Vector3 velocityCappedExcess = CalculateVelocityCaps(currentVelocity);
        rb.AddForce(velocityCappedExcess, ForceMode.VelocityChange);
        currentVelocity += velocityCappedExcess;
        // Debug.Log($"Current Velocity: {rb.linearVelocity:F2}\t Desired Acc: {desiredAcc:F2}\t Jet Resistance: {jetResistance:F2}\t Capped Excess: {velocityCappedExcess:F2}\t Final Velocity: {currentVelocity:F2}");
    }

    private Vector3 CalculateJetResistance(Vector3 currentVelocity, float desiredVerticalAcc, Vector3 desiredAcc)
    {
        Vector3 newVelocity = currentVelocity;

        // Vertical
        float currentVerticalSpeed = Mathf.Abs(currentVelocity.y);
        float currentDesiredVerticalAccel = Mathf.Abs(desiredVerticalAcc);

        if (currentVerticalSpeed > 0.0f && currentDesiredVerticalAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentVerticalSpeed, verticalJetResistanceFactor) * verticalJetResistance, 0.0f, 0.25f);
            newVelocity.y *= (currentVerticalSpeed - resist * currentDesiredVerticalAccel) / currentVerticalSpeed;
        }

        // Horizontal
        Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float currentHorizontalSpeed = currentHorizontalVelocity.magnitude;
        float currentDesiredAccel = desiredAcc.magnitude;

        if (currentHorizontalSpeed > 0.0f && currentDesiredAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentHorizontalSpeed, horizontalJetResistanceFactor) * horizontalJetResistance, 0.0f, 0.925f);
            float scale = (currentHorizontalSpeed - resist * currentDesiredAccel) / currentHorizontalSpeed;
            newVelocity.x *= scale;
            newVelocity.z *= scale;
        }

        return newVelocity - currentVelocity;
    }

    private Vector3 CalculateVelocityCaps(Vector3 currentVelocity)
    {
        // Horizontal Velocity Cap
        Vector3 cappedVelocity = currentVelocity;
        Vector3 horizVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float horizSpeed = horizVelocity.magnitude;
        if (horizSpeed > horizResistSpeed * 3.0f)
        {
            float maxSpeed = horizMaxSpeed * 3.0f;
            float targetSpeed = Mathf.Min(horizSpeed, maxSpeed);

            float scale = (targetSpeed - Time.fixedDeltaTime * horizResistFactor * (targetSpeed - horizResistSpeed)) / horizSpeed;
            cappedVelocity.x *= scale;
            cappedVelocity.z *= scale;
        }

        // Upward Velocity Cap
        if (cappedVelocity.y > upResistSpeed)
        {
            if (cappedVelocity.y > upMaxSpeed)
                cappedVelocity.y = upMaxSpeed;

            cappedVelocity.y -= Time.fixedDeltaTime * upResistFactor * (cappedVelocity.y - upResistSpeed);
        }

        // Downward Velocity Cap
        if (cappedVelocity.y < -downResistSpeed)
        {
            if (cappedVelocity.y < -downMaxSpeed)
                cappedVelocity.y = -downMaxSpeed;

            cappedVelocity.y += Time.fixedDeltaTime * downResistFactor * (cappedVelocity.y + downResistSpeed);
        }

        return cappedVelocity - currentVelocity;
    }

    private void HandleRotation()
    {
        Vector2 rotationInput = playerControls.Movement.Look.ReadValue<Vector2>();
        Vector3 rotation = new(0f, rotationInput.x, 0f);
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
                    value => { field.SetValue(playerTelemetry, value); }
                );
            }
        }
    }

    protected override void OnDie()
    {
        print("Player Died");
    }
}
