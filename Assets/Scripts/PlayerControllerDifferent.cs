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

    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float rotationLimit = 20f;

    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    private PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private TerrainDetector terrainDetector;
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;

    [Range(0.0f, 1.0f)]
    [SerializeField ] private float hoverHeightMax = 0.4f;

    [Range(0.0f, 10.0f)]
    [SerializeField ] private float hoverStrength = 1;
    [Range(0.0f, 10.0f)]
    [SerializeField ] private float hoverBaseForce = 1;
    

    [Range(0.0f, 100.0f)]
    [SerializeField ] private float dampeningStrength = 10;



    [Range(0.0f, 300f)]
    [SerializeField ] private float skiStrength = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float resistSkiSpeed = 40f;

    [Range(0.0f, 300f)]
    [SerializeField ] private float maxSkiSpeed = 120f;


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

        // Calculate terrain data from collision points
        if (terrainContactPoints.Count > 0)
        {
            surfaceNormal = Vector3.zero;
            int i = 0;
            foreach (ContactPoint contact in terrainContactPoints)
            {
                Debug.DrawRay(contact.point - contact.normal * contact.separation, contact.normal * 2.0f, Color.Lerp(Color.red, Color.green, ((float)i/(float)terrainContactPoints.Count)), 5.0f);
                surfaceNormal += contact.normal;
                surfacePoint += (contact.point - contact.normal * contact.separation);
                // distanceToSurface = Mathf.Min(distanceToSurface, contact.separation);
                i++;
            }
            surfaceNormal /= terrainContactPoints.Count;
            surfacePoint /= terrainContactPoints.Count;
            // distanceToSurface += 0.6f; // difference between inner and outer radius of capsule collider
            // distanceToSurface = Mathf.Max(distanceToSurface, 0.0f);
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

                Debug.DrawRay(hit.point, hit.normal * 2.0f, Color.cyan, 5.0f);
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
            //         distanceToSurface = Mathf.Min(Vector3.Distance(hit.point, playerCollider.bounds.ClosestPoint(hit.point)), distanceToSurface);

            //         surfaceNormal = (surfaceNormal + hit.normal) / 2.0f;
            //         surfacePoint = (surfacePoint + hit.point) / 2.0f;

            //         Debug.DrawRay(hit.point, hit.normal * 2.0f, Color.magenta, 5.0f);
            //     }
            // }
        }

        float distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, playerCollider.bounds.ClosestPoint(surfacePoint)), 0.0f);
        if (distanceToSurface < 0.6f) // difference between inner and outer radius of capsule collider
        {
            float angle = Vector3.Angle(Vector3.up, surfaceNormal);
            isGrounded = angle < maxRunUpSurfaceAngle;
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
            // if (distanceToSurface < hoverHeightMax * 5.0f)
            Vector3 velocityDelta = rb.velocity - lastVelocity;
            // if (distanceToSurface < hoverHeightMax * (1.0f + velocityDelta.magnitude))
            // {
                // Hover force that opposes gravity
                float hoverForce = Mathf.Max((distanceToSurface - hoverHeightMax) * -hoverStrength + (Physics.gravity.magnitude * Time.fixedDeltaTime + hoverBaseForce), 0.0f);
                // float hoverForceMultiplier = 1.0f - ((distanceToSurface - hoverHeightMax) / hoverHeightMax);
                // float hoverForce = hoverForceMultiplier * hoverStrength;

                // Dampening force

                float surfaceDistanceDelta = distanceToSurface - lastSurfaceDistance;
                // float dampeningForce = (surfaceDistanceDelta * -dampeningStrength);
                float dampeningForce = (surfaceDistanceDelta * -dampeningStrength) * Mathf.Max(1.0f -  (distanceToSurface / 2.0f), 0.0f);

                // float surfaceDistanceDelta = lastSurfaceDistance - distanceToSurface;
                // float dampeningForceMultiplier = Mathf.Clamp(surfaceDistanceDelta/Mathf.Max(0.00001f, (2 * hoverHeightMax) - distanceToSurface), -1.0f, 1.0f);
                // float dampeningForce = dampeningForceMultiplier * dampeningStrength;

                // float speedDeltaTowardsSurface = Vector3.Dot(velocityDelta, -surfaceNormal);
                // float speedTowardsSurface = 1.0f + Mathf.Max(Vector3.Dot(rb.velocity, -surfaceNormal), 0.0f);
                // float dampeningForce = Mathf.Pow(distanceToSurface - hoverHeightMax, 2) * surfaceDistanceDelta * Mathf.Sqrt(speedTowardsSurface) * dampeningStrength;
                // float dampeningForce = (distanceToSurface - hoverHeightMax) * dampeningStrength * Mathf.Min(speedDeltaTowardsSurface, 0.0f);
                
                // dampeningForce = Mathf.Max(dampeningForce, -hoverForce);

                // Combining and constraining forces
                float combinedForce = Mathf.Max(hoverForce + dampeningForce, 0.0f);
                // combinedForce = Mathf.Min(combinedForce, dampeningForce);
                // combinedForce = Mathf.Max(combinedForce, hoverForce);

                rb.AddForce(combinedForce * surfaceNormal, ForceMode.VelocityChange);
                // rb.AddForce((combinedForce * surfaceNormal) - Vector3.Project(rb.velocity, surfaceNormal), ForceMode.VelocityChange);

                // Debug.DrawRay(playerPositionCenter, hoverForce/10f * surfaceNormal, Color.green, 10.0f);
                // Debug.DrawRay(playerPositionCenter, dampeningForce/10f * surfaceNormal, Color.red, 10.0f);
                Debug.DrawRay(playerPositionCenter, combinedForce/10f * surfaceNormal, Color.blue, 10.0f);
                Debug.DrawRay(playerPositionCenter, transform.right, Color.yellow, 10.0f);

                // Debug.Log($"Distance: {distanceToSurface}Hover: {hoverForce}, Dampening: {dampeningForce}, speedDeltaTowardsSurface: {speedDeltaTowardsSurface}, Velocity: {rb.velocity.magnitude}");
                Debug.Log($"Delta: {surfaceDistanceDelta}, Distance: {distanceToSurface}, Last Distance: {lastSurfaceDistance}, Ground Normal: {surfaceNormal}, Hover: {hoverForce}, Dampening: {dampeningForce}, Combined: {combinedForce * surfaceNormal * Time.fixedDeltaTime}, Velocity: {rb.velocity.magnitude}");
            // }

            // Jetting Force
            Vector3 jetForce = Vector3.zero;

            // Horizontal Jetting Force
            float currentLateralSpeed = Vector3.Dot(rb.velocity, movementDirectionAdjusted);
            float skiResistMultiplier = Mathf.Max(1.0f - ((currentLateralSpeed - resistSkiSpeed) / (maxSkiSpeed - resistSkiSpeed)), 0.0f);
            jetForce += movementDirectionAdjusted * skiStrength * skiResistMultiplier;

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
