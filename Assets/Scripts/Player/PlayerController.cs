using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTelemetry
{
    [PauseMenuDevOption("Raw Collision Data")]
    public bool enableRawCollisionDebug = false;

    [PauseMenuDevOption("Surface Data")]
    public bool enableSurfaceDebug = false;

    [PauseMenuDevOption("Movement Data")]
    public bool enableMovementDebug = false;


    private DevVectorRenderer devVectorRenderer;

    public Vector3 position;
    public Vector3 velocity;

    public Vector3 movementDirection;

    public List<Vector3> rawCollisionPoints;
    public List<Vector3> rawCollisionDirections;
    public List<Vector3> surfaceQueryPoints;
    public List<Vector3> surfaceQueryDirections;
    public Vector3 surfaceNormal;
    public Vector3 surfacePoint;


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
        this.rawCollisionPoints = new List<Vector3>();
        this.rawCollisionDirections = new List<Vector3>();
        this.surfaceQueryPoints = new List<Vector3>();
        this.surfaceQueryDirections = new List<Vector3>();
        this.surfaceNormal = Vector3.zero;
        this.surfacePoint = Vector3.zero;
        this.isSkiing = false;
        this.isUpJetting = false;
        this.isDownJetting = false;
        this.isGrounded = false;
    }

    public void Update()
    {

        if (enableRawCollisionDebug)
        {
            for (int i = 0; i < rawCollisionPoints.Count; i++)
            {
                devVectorRenderer.AddDevVector(rawCollisionPoints[i], rawCollisionDirections[i], Color.Lerp(Color.red, Color.green, ((float)i / (float)rawCollisionPoints.Count)), 5.0f);
            }

            for (int i = 0; i < surfaceQueryPoints.Count; i++)
            {
                devVectorRenderer.AddDevVector(surfaceQueryPoints[i], surfaceQueryDirections[i], Color.Lerp(Color.red, Color.green, ((float)i / (float)surfaceQueryPoints.Count)), 1.0f);
            }
        }

        if (enableSurfaceDebug)
        {
            devVectorRenderer.AddDevVector(surfacePoint, surfaceNormal * 0.5f, new Color(0f, 1f, 0f, 0.2f), 5.0f, 0.1f);

            DebugWidgetManager.Instance.SetDebugText("Terrain Surface",
            $"Point: {surfacePoint.ToString("F2")}\nNormal: {surfaceNormal.ToString("F2")}",
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
            $"Speed: {velocity.magnitude.ToString("F2")}\nDirection: {velocity.normalized.ToString("F0")}\nDesired Direction: {movementDirection.ToString("F0")}\nIs Grounded: {isGrounded}\nIs Skiing: {isSkiing}\nIs Up Jetting: {isUpJetting}\nIs Down Jetting: {isDownJetting}",
            100, -300);
        }
        else
        {
            DebugWidgetManager.Instance.RemoveDebugText("Velocity");
        }

        // Clear all the lists
        rawCollisionPoints.Clear();
        rawCollisionDirections.Clear();
        surfaceQueryPoints.Clear();
        surfaceQueryDirections.Clear();
    }
}


public class PlayerController : MonoBehaviour
{
    [SerializeField] private DevVectorRenderer devVectorRenderer;

    public PlayerTelemetry playerTelemetry;

    private float drag = 0.004f;                        
    private float airCushionDrag = 0.00275f;             
    private float airCushionHeight = 10f;    
    // private float maxRunUpSurfaceAngle = 50f; 
    private float mass = 75f;

    [PauseMenuOption("Horizontal Look", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;
    private float horizontalRotationLimit = 100f;

    [PauseMenuOption("Vertical Look", 0f, 100f)]
    public float verticalRotationSpeed = 60f;
    public float verticalRotationLimit = 100f;

    [Header("Physics")]
    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    public PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private TerrainDetector terrainDetector;
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;


    [Header("Hovering")]
    [Range(0.0f, 2.0f)]
    [SerializeField] private float hoverHeightMax = 0.2f;

    [Header("Skiing")]
    // public static float skiStrengthMin = 0f;
    // public static float skiStrengthMax = 300f;
    [PauseMenuDevOption("Ski Force", 0f, 300f)]
    [SerializeField] public float skiStrength = 40f;

    [PauseMenuDevOption("Skiing Resist Speed", 0f, 300f)]
    [SerializeField] public float resistSkiSpeed = 40f;

    [PauseMenuDevOption("Skiing Max Speed", 0f, 300f)]
    [SerializeField] public float maxSkiSpeed = 120f;


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

    private Vector3 lastKnownSurfaceNormal = Vector3.zero;
    private Vector3 lastKnownSurfacePoint = Vector3.zero;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask ignoreLayers;


    bool skiToggleInput = false;
    bool skiToggle = false;
    bool isJumping = false;

    public bool hasFocus = false;

    [SerializeField] private Transform playerUI;

    [SerializeField] private Transform weaponMountPoint;
    [SerializeField] private Transform throwableMountPoint;

    private PlayerLoadout playerLoadout;

    void Awake()
    {
        playerTelemetry = new PlayerTelemetry(devVectorRenderer);

        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.0f;
        rb.mass = mass;
        playerCollider = GetComponent<CapsuleCollider>();
        playerCollider.material = normalMaterial;
        terrainDetector = transform.parent.GetComponentInChildren<TerrainDetector>();
        animator = GetComponent<Animator>();
        playerLoadout = GetComponent<PlayerLoadout>();
        playerLoadout.Initialize(this, weaponMountPoint, throwableMountPoint);
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

    void Update()
    {
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
    }

    void FixedUpdate()
    {
        HandleMovement();
        if (hasFocus) HandleRotation();
        playerTelemetry.position = transform.position;
        playerTelemetry.velocity = rb.velocity;
    }

    private void HandleMovement()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.Move.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        // Get direction of movement relative to player rotation
        Vector3 movementDirection = transform.TransformDirection(movement).normalized;
    
        // Get input for skiing, jumping, and down jetting
        bool isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f || skiToggle;
        bool isUpJetting = isSkiing && playerControls.Movement.JumpJet.ReadValue<float>() > 0.0f;
        bool isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;
        bool isJetting = isUpJetting || isDownJetting;
        bool isMoving = movement.magnitude > 0.0f;
        bool isGrounded = false;

        playerTelemetry.movementDirection = movementDirection;
        playerTelemetry.isSkiing = isSkiing;
        playerTelemetry.isUpJetting = isUpJetting;
        playerTelemetry.isDownJetting = isDownJetting;

        // Set friction based on whether player is skiing or not
        playerCollider.material = isSkiing ? skiMaterial : normalMaterial;

        // Get terrain points from terrain detector
        List<ContactPoint> terrainContactPoints = terrainDetector.GetTerrainContactPoints();

        Vector3 playerPositionCenter = playerCollider.bounds.center;
        
        Vector3 surfaceNormal = Vector3.up;
        Vector3 surfacePoint = Vector3.zero;

        float distanceToSurface = Mathf.Infinity;

        float surfaceDetectionResolution = 15f;

        // Calculate terrain data from collision points
        if (terrainContactPoints.Count > 0)
        {

            // Get average surface normal and point from all contact points
            surfaceNormal = Vector3.zero;
            int i = 0;
            foreach (ContactPoint contact in terrainContactPoints)
            {
                playerTelemetry.rawCollisionPoints.Add(contact.point - contact.normal * contact.separation);
                playerTelemetry.rawCollisionDirections.Add(contact.normal);
                surfaceNormal += contact.normal;
                surfacePoint += (contact.point - contact.normal * contact.separation);
                i++;
            }
            surfaceNormal /= terrainContactPoints.Count;
            surfacePoint /= terrainContactPoints.Count;

            // Do a bunch of raycasts at player height intervals to get a more accurate surface normal and distance to surface
            float playerHeight = playerCollider.bounds.size.y;
            Vector3 playerBottom = playerPositionCenter - Vector3.up * playerHeight / 2.0f;
            Vector3 playerTop = playerPositionCenter + Vector3.up * playerHeight / 2.0f;
            Vector3 checkDirection = -surfaceNormal;

            for (i = 0; i < surfaceDetectionResolution; i++)
            {
                Vector3 playerHeightPoint = Vector3.Lerp(playerTop, playerBottom, (float)i / (float)surfaceDetectionResolution);
                RaycastHit hit;
                bool didHit = Physics.Raycast(
                    new Ray(
                        playerHeightPoint,
                        checkDirection
                    ),
                    out hit,
                    Mathf.Infinity,
                    ~ignoreLayers
                );
                if (didHit)
                {
                    playerTelemetry.surfaceQueryPoints.Add(hit.point);
                    playerTelemetry.surfaceQueryDirections.Add(hit.normal);

                    float hitDistance = Vector3.Distance(hit.point, playerCollider.ClosestPoint(hit.point));
                    if (hitDistance < distanceToSurface)
                    {
                        surfaceNormal = hit.normal;
                        surfacePoint = hit.point;
                        distanceToSurface = hitDistance;
                    }
                }
            }
        }
        // Fall back to raycasting if no collision points
        else
        {
            // Raycast to last known ground location
            RaycastHit hit;
            bool didHit = Physics.Raycast(
                new Ray(
                    playerPositionCenter,
                    -lastKnownSurfaceNormal
                ),
                out hit,
                Mathf.Infinity,
                ~ignoreLayers
            );
            if (didHit)
            {
                surfaceNormal = hit.normal;
                surfacePoint = hit.point;

                distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, playerCollider.ClosestPoint(surfacePoint)), 0.0f);

                playerTelemetry.rawCollisionPoints.Add(hit.point);
                playerTelemetry.rawCollisionDirections.Add(hit.normal);
            }
        }

        if (distanceToSurface < hoverHeightMax * 2.0f)
        {
            isGrounded = true;
        }

        if (Vector3.Distance(lastKnownSurfaceNormal, surfaceNormal) > 0.00001f)
        {
            float surfaceNormalLerpFactor = Mathf.Clamp01((distanceToSurface - 1.0f) / 10.0f);
            lastKnownSurfaceNormal = Vector3.Lerp(surfaceNormal, Vector3.up, surfaceNormalLerpFactor);
        }
        if (Vector3.Distance(lastKnownSurfacePoint, surfacePoint) > 0.00001f) lastKnownSurfacePoint = surfacePoint;

        playerTelemetry.isGrounded = isGrounded;
        playerTelemetry.surfacePoint = surfacePoint;
        playerTelemetry.surfaceNormal = surfaceNormal;

        bool isRunning = isGrounded && isMoving && !isSkiing;


        // Apply Drag
        float chosenDrag = 0.0f;
        if (distanceToSurface <= airCushionHeight)
        {
            chosenDrag = airCushionDrag;
        }
        else
        {
            chosenDrag = drag;
        }
        rb.drag = chosenDrag;

        Vector3 movementDirectionAdjusted = Vector3.ProjectOnPlane(movementDirection, surfaceNormal).normalized;


        // Set animator values
        Vector3 animMovementDirectionNewY = Vector3.up * (isDownJetting ? -1f : (isUpJetting ? 1f : 0f));
        animMovementDirection = Vector3.Lerp(animMovementDirection, movement.normalized + animMovementDirectionNewY, Time.fixedDeltaTime * 10f);
        animator.SetFloat("xDir", animMovementDirection.x);
        animator.SetFloat("yDir", animMovementDirection.y);
        animator.SetFloat("zDir", animMovementDirection.z);
        animator.SetFloat("yVel", rb.velocity.normalized.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isSkiing", isSkiing && (!isUpJetting && !isDownJetting));
        animator.SetBool("isJetting", isUpJetting || isDownJetting);


        // Skiing Movement
        if (isSkiing)
        {
            // Hovering
            float hoverFactor = Mathf.Clamp01(1.0f - (distanceToSurface - hoverHeightMax)/hoverHeightMax) * 1.1f;
            float baseForce = Physics.gravity.magnitude * hoverFactor;

            Vector3 lateralMovementDirection = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized;
            Vector3 lateralSurfaceDirection = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up).normalized;
            float slopeDirection = Vector3.Dot(lateralMovementDirection, lateralSurfaceDirection);


            Vector3 hoverForce = Vector3.zero;

            // Constant "up" force
            hoverForce.y = baseForce;

            // Going downhill
            if (slopeDirection > 0.0f)
            {
                // Twice as much force in the direction of the slope
                hoverForce.x = surfaceNormal.x * 2.0f * baseForce;
                hoverForce.z = surfaceNormal.z * 2.0f * baseForce;
            }
            // Going uphill
            else
            {
                // Adjusts the surface normal depending on how steep the slope is, steeper slopes make the normal more vertical
                Vector3 adjustedSurfaceNormal = surfaceNormal - lateralMovementDirection * slopeDirection;

                Vector3 latMoveDirOrUp = lateralMovementDirection.magnitude > 0.0f ? lateralMovementDirection : Vector3.up;
                float negVelocityDirOrUpDotSurfaceNormal = Vector3.Dot(-latMoveDirOrUp, adjustedSurfaceNormal);

                // Half as much force in the direction of the slope
                hoverForce.x = (adjustedSurfaceNormal.x + latMoveDirOrUp.x * negVelocityDirOrUpDotSurfaceNormal) * 0.5f * baseForce;
                hoverForce.z = (adjustedSurfaceNormal.z + latMoveDirOrUp.z * negVelocityDirOrUpDotSurfaceNormal) * 0.5f * baseForce;
            }

            rb.AddForce(hoverForce, ForceMode.Acceleration);


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
            if (isUpJetting)jetForce += Vector3.up * upJetStrength * verticalJetResistMultiplier;
            if (isDownJetting) jetForce += -Vector3.up * downJetStrength * verticalJetResistMultiplier;

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
        transform.Rotate(rotation);
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
}
