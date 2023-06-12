using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerDifferent : MonoBehaviour
{
    private float drag = 0.004f;                        
    private float airCushionDrag = 0.00275f;             
    private float airCushionHeight = 10f;    
    private float maxRunUpSurfaceAngle = 50f; 
    private float mass = 75f;

    private float rotationSpeed = 20f;
    private float rotationLimit = 20f;

    [Header("Physics")]
    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    private PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private TerrainDetector terrainDetector;
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;


    [Header("Hovering")]
    [Range(0.0f, 2.0f)]
    [SerializeField ] private float hoverHeightMax = 0.6f;
    // [Range(0.0f, 100.0f)]
    // [SerializeField ] private float hoverStrength = 61;
    [SerializeField ] private AnimationCurve hoverCurve;
    // [Range(0.0f, 50.0f)]
    // [SerializeField ] private float hoverBaseForce = 30;

    // [Range(0.0f, 300.0f)]
    // [SerializeField ] private float dampeningStrength = 150;
    [SerializeField ] private AnimationCurve dampeningCurve;


    [Header("Skiing")]
    [Range(0.0f, 300f)]
    [SerializeField ] private float skiStrength = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float resistSkiSpeed = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float maxSkiSpeed = 120f;


    [Header("Jetting")]
    [Range(0.0f, 300f)]
    [SerializeField ] private float upJetStrength = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float resistUpJetSpeed = 60f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float maxUpJetSpeed = 120f;

    
    [Range(0.0f, 300f)]
    [SerializeField ] private float downJetStrength = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float resistDownJetSpeed = 60f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float maxDownJetSpeed = 120f;

    private Vector3 lastKnownSurfaceNormal = Vector3.zero;
    private Vector3 lastKnownSurfacePoint = Vector3.zero;
    private float lastKnownSurfaceCounter = 0;
    private float lastSurfaceDistance = 0.0f;

    [Header("Collision Detection")]
    [SerializeField ] private LayerMask ignoreLayers;

    private Vector3 lastPlayerPosition = Vector3.zero;
    private Vector3 lastVelocity = Vector3.zero;

    bool skiToggleInput = false;
    bool skiToggle = false;

    bool hasFocus = false;

    void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.0f;
        rb.mass = mass;
        playerCollider = GetComponent<CapsuleCollider>();
        playerCollider.material = normalMaterial;
        terrainDetector = transform.parent.GetComponentInChildren<TerrainDetector>();
        animator = GetComponent<Animator>();
    }

    void OnApplicationFocus(bool tempHasFocus)
    {
        if (tempHasFocus) Cursor.lockState = CursorLockMode.Locked;
        hasFocus = tempHasFocus;
    }
    void OnEnable() { playerControls.Enable(); }
    void OnDisable() { playerControls.Disable(); }

    void Update()
    {
        bool tempSkiToggleInput = playerControls.Movement.ToggleSki.ReadValue<float>() > 0.0f;
        if (tempSkiToggleInput && !skiToggleInput)
        {
            skiToggle = !skiToggle;
        }
        skiToggleInput = tempSkiToggleInput;
    }

    void FixedUpdate()
    {
        HandleMovement();
        if (hasFocus) HandleRotation();
        lastVelocity = rb.velocity;
    }

    private void HandleMovement()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.MoveVector.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        // Get direction of movement relative to player rotation
        Vector3 movementDirection = transform.TransformDirection(movement).normalized;
    
        // Get input for skiing, jumping, and down jetting
        bool isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f || skiToggle;
        bool isJumping = playerControls.Movement.Jump.ReadValue<float>() > 0.0f;
        bool isUpJetting = isSkiing && isJumping;
        bool isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;
        bool isJetting = isUpJetting || isDownJetting;
        bool isMoving = movement.magnitude > 0.0f;
        bool isGrounded = false;

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
            surfaceNormal = Vector3.zero;
            int i = 0;
            foreach (ContactPoint contact in terrainContactPoints)
            {
                // Debug.DrawRay(contact.point - contact.normal * contact.separation, contact.normal * 2.0f, Color.Lerp(Color.red, Color.green, ((float)i/(float)terrainContactPoints.Count)), 5.0f);
                surfaceNormal += contact.normal;
                surfacePoint += (contact.point - contact.normal * contact.separation);
                distanceToSurface = Mathf.Min(distanceToSurface, Mathf.Max(Vector3.Distance(surfacePoint, playerCollider.ClosestPoint(surfacePoint)), 0.0f));
                i++;
            }
            surfaceNormal /= terrainContactPoints.Count;
            surfacePoint /= terrainContactPoints.Count;
            // distanceToSurface += 0.6f; // difference between inner and outer radius of capsule collider
            // distanceToSurface = Mathf.Max(distanceToSurface, 0.0f);


            // Do a bunch of raycasts at player height intervals to get a more accurate surface normal
            float playerHeight = playerCollider.bounds.size.y;
            Vector3 playerBottom = playerPositionCenter - Vector3.up * playerHeight / 2.0f;
            Vector3 playerTop = playerPositionCenter + Vector3.up * playerHeight / 2.0f;
            Vector3 checkDirection = -surfaceNormal;

            print("testing");
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
                    Debug.DrawRay(hit.point, hit.normal * 2.0f, Color.Lerp(Color.red, Color.green, ((float)i / (float)surfaceDetectionResolution)), 1.0f);
                    Debug.DrawRay(playerCollider.ClosestPoint(hit.point), Vector3.up, Color.Lerp(Color.red, Color.green, ((float)i / (float)surfaceDetectionResolution)), 1.0f);
                    float hitDistance = Vector3.Distance(hit.point, playerCollider.ClosestPoint(hit.point));
                    print(hitDistance);
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

                // Debug.DrawRay(hit.point, hit.normal * 2.0f, Color.cyan, 5.0f);
                // Debug.Log(hit.transform.gameObject.name + " " + hit.normal + " " + hit.point + " " + hit.distance);
            }

            // // Raycast looking towards player velocity direction
            // if (rb.velocity.magnitude > 0.0f)
            // {
            //     Vector3 raycastDirection = (rb.velocity.normalized * 0.5f) + (-lastKnownSurfaceNormal * 0.5f);
            //     didHit = Physics.Raycast(
            //         new Ray(
            //             playerPositionCenter,
            //             raycastDirection
            //         ),
            //     out hit,
            //     Mathf.Min(rb.velocity.magnitude * 0.2f, 2.0f));
            //     if (didHit)
            //     {
            //         distanceToSurface = Mathf.Min(Vector3.Distance(hit.point, playerCollider.ClosestPoint(hit.point)), distanceToSurface);

            //         surfaceNormal = (surfaceNormal + hit.normal) / 2.0f;
            //         surfacePoint = (surfacePoint + hit.point) / 2.0f;

            //         Debug.DrawRay(hit.point, hit.normal * 2.0f, Color.magenta, 5.0f);
            //     }
            // }
        }

        if (distanceToSurface < hoverHeightMax * 2.0f) // difference between inner and outer radius of capsule collider
        {
            isGrounded = true;
        }

        // Vector3 playerPositionDelta = playerPositionCenter - lastPlayerPosition;
        // lastSurfaceDistance = distanceToSurface + Vector3.Project(playerPositionDelta, -surfaceNormal).magnitude;
        // Debug.Log(lastSurfaceDistance + " " + distanceToSurface + " " + Vector3.Dot(playerPositionDelta, surfaceNormal) + " " + playerPositionDelta + " " + surfaceNormal + " " + playerPositionCenter + " " + lastPlayerPosition);

        if (Vector3.Distance(lastKnownSurfaceNormal, surfaceNormal) > 0.00001f)
        {
            float surfaceNormalLerpFactor = Mathf.Clamp01((distanceToSurface - 1.0f) / 10.0f);
            lastKnownSurfaceNormal = Vector3.Lerp(surfaceNormal, Vector3.up, surfaceNormalLerpFactor);
            // lastKnownSurfaceNormal = surfaceNormal;
        }
        if (Vector3.Distance(lastKnownSurfacePoint, surfacePoint) > 0.00001f) lastKnownSurfacePoint = surfacePoint;

        Debug.DrawRay(surfacePoint, surfaceNormal, Color.blue, 5.0f);

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
            // Vector3 velocityDelta = rb.velocity - lastVelocity;

            // // Hover force that opposes gravity
            // float hoverForce = hoverCurve.Evaluate(distanceToSurface/hoverHeightMax) * hoverStrength;

            // // Dampening force
            // float surfaceDistanceDelta = distanceToSurface - lastSurfaceDistance;
            // float dampeningForce = dampeningCurve.Evaluate(surfaceDistanceDelta) * dampeningStrength / (1.0f + distanceToSurface);

            // // Combining forces
            // float combinedForce = Mathf.Max(hoverForce + dampeningForce, 0.0f);
            // Vector3 combinedForceVector = combinedForce * surfaceNormal;

            // rb.AddForce(combinedForceVector, ForceMode.Acceleration);

            // --------------------------------------

            float hoverFactor = Mathf.Clamp01(1.0f - (distanceToSurface - hoverHeightMax)/hoverHeightMax) * 1.1f;

            Vector3 VelocityDirNoY = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized;
            float velocityDirIntoSurface = Vector3.Dot(VelocityDirNoY, surfaceNormal);

            float forceX = 0.0f;
            float forceY = 0.0f;
            float forceZ = 0.0f;

            if (velocityDirIntoSurface > 0.0f)
            {
                forceX = 0.0f - 2.0f * -55.0f * surfaceNormal.x * Time.fixedDeltaTime * hoverFactor;
                forceY = 55.0f * Time.fixedDeltaTime * hoverFactor;
                forceZ = 0.0f - 2.0f * -55.0f * surfaceNormal.z * Time.fixedDeltaTime * hoverFactor;
            }
            else
            {
                Vector3 adjustedSurfaceNormal = surfaceNormal - VelocityDirNoY * velocityDirIntoSurface;

                Vector3 velocityDirOrUp = rb.velocity.magnitude > 0.0f ? rb.velocity.normalized : Vector3.up;
                float negVelocityDirOrUpDotSurfaceNormal = Vector3.Dot(-velocityDirOrUp, surfaceNormal);

                forceX = 0.0f - (adjustedSurfaceNormal.x - velocityDirOrUp.x * negVelocityDirOrUpDotSurfaceNormal * -1.0f) * 0.5f * -55.0f * Time.fixedDeltaTime * hoverFactor;
                forceY = 55.0f * Time.fixedDeltaTime * hoverFactor;
                forceZ = 0.0f - (adjustedSurfaceNormal.z - velocityDirOrUp.z * negVelocityDirOrUpDotSurfaceNormal * -1.0f) * 0.5f * -55.0f * Time.fixedDeltaTime * hoverFactor;
            }

            Vector3 forceVector = new Vector3(forceX, forceY, forceZ);
            rb.AddForce(forceVector, ForceMode.VelocityChange);

            // Debug.DrawRay(playerPositionCenter, forceVector, Color.red, 10.0f);
            print(distanceToSurface + " " + forceVector + " " + surfaceNormal);

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

        lastPlayerPosition = playerPositionCenter;
        lastSurfaceDistance = distanceToSurface;
    }

    private void HandleRotation()
    {
        Vector2 rotationInput = playerControls.Movement.LookVector.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(0f, rotationInput.x, 0f);
        rotation *= rotationSpeed * Time.fixedDeltaTime;
        rotation = Vector3.ClampMagnitude(rotation, rotationLimit);
        transform.Rotate(rotation);
    }
}
