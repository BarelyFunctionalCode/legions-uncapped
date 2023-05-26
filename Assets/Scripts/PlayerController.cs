using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float drag = 0.004f;                        
    private float airCushionDrag = 0.00275f;             
    private float airCushionHeight = 10f;    

    private float horizontalJetResistance = 0.0017f;
    private float horizontalJetResistanceFactor = 1.8f;
    private float verticalJetResistance = 0.0006f;
    private float verticalJetResistanceFactor = 1.8f;

    private float runForce = 10000f;
    private float maxRunForwardSpeed = 20f;
    private float maxRunBackwardSpeed = 20f;
    private float maxRunSideSpeed = 20f;
    private float maxRunUpSurfaceAngle = 50f; 

    // Forces used while jetting/skiing
    private float horizontalJetForce = 3125f;
    private float upJetForce = 7031.25f;
    private float downJetForce = 5156.25f;

    // Final Speed Caps
    private float horizontalResistFactor = 0.3f;
    private float horizontalResistSpeed = 100f;              // <---- I think this means that "at this speed, the player will be slowed down by the horizResistFactor"
    private float horizontalMaxSpeed = 120f;
    private float upResistFactor = 0.3f;
    private float upResistSpeed = 85f;                  // <---- Same as above
    private float upMaxSpeed = 115f;
    private float downResistFactor = 0.1f;
    private float downResistSpeed = 1000f;
    private float downMaxSpeed = 1000f;                  //<---- Max speeds for each direction (velocity magnitude)

    // Acceleration multipliers based on current speed
    private float jetAirMoveMinSpeed = 5;
    private float jetAirMoveMaxSpeed = 1000;
    private float jetAirMoveMaxAccelFactor = 1.5f;
    
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
    [SerializeField ] private float hoverHeightMax = 0.6f;

    [Range(0.0f, 20000.0f)]
    [SerializeField] private float hoverStrength = 65f;

    [Range(0.0f, 20000.0f)]
    [SerializeField] private float hoverDampening = 1000f;

    private Vector3 lastKnownGroundedNormal = Vector3.zero;
    private Vector3 lastKnownGroundedPoint = Vector3.zero;
    private float lastKnownGroundCounter = 0;
    private float lastGroundDistance = 0.0f;

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
        terrainDetector = GetComponentInChildren<TerrainDetector>();
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
        
        Vector3 groundNormal = Vector3.up;
        Vector3 groundPoint = Vector3.zero;
        float distanceToGround = Mathf.Infinity;

        // Calculate terrain data from collision points
        if (terrainContactPoints.Count > 0)
        {
            groundNormal = Vector3.zero;
            int i = 0;
            foreach (ContactPoint contact in terrainContactPoints)
            {
                Debug.DrawRay(contact.point - contact.normal * contact.separation, contact.normal, Color.Lerp(Color.red, Color.green, ((float)i/(float)terrainContactPoints.Count)), 1.0f);
                groundNormal += contact.normal;
                groundPoint += (contact.point - contact.normal * contact.separation);
                distanceToGround = Mathf.Min(distanceToGround, contact.separation);
                i++;
            }
            groundNormal /= terrainContactPoints.Count;
            groundPoint /= terrainContactPoints.Count;
            distanceToGround += 0.6f; // difference between inner and outer radius of capsule collider
            // distanceToGround = Mathf.Clamp(distanceToGround, 0.0f, hoverHeightMax);

            lastKnownGroundedNormal = groundNormal;
            lastKnownGroundedPoint = groundPoint;

            float angle = Vector3.Angle(Vector3.up, groundNormal);
            isGrounded = angle < maxRunUpSurfaceAngle;
        }
        // Fall back to raycasting if no collision points
        else
        {
            if (lastKnownGroundCounter < 0.5f)
            {
                lastKnownGroundCounter += Time.fixedDeltaTime;
            }
            else
            {
                lastKnownGroundCounter = 0.0f;
                lastKnownGroundedNormal = Vector3.up;
                lastKnownGroundedPoint = Vector3.zero;
            }

            RaycastHit hit;
            bool didHit = Physics.Raycast(
                new Ray(
                    playerPositionCenter,
                    -lastKnownGroundedNormal
                ),
            out hit);
            if (didHit)
            {
                distanceToGround = Vector3.Distance(hit.point, playerCollider.bounds.ClosestPoint(hit.point));
                if (distanceToGround < 0.6f) // difference between inner and outer radius of capsule collider
                {
                    groundNormal = hit.normal;
                    groundPoint = hit.point;

                    lastKnownGroundedNormal = groundNormal;
                    lastKnownGroundedPoint = groundPoint;

                    isGrounded = true;
                }
            }
        }

        Debug.DrawRay(groundPoint, groundNormal, Color.blue, 5.0f);

        bool isRunning = isGrounded && isMoving && !isSkiing;


        // Apply Horizontal Resistance
        float currentHorizontalMagnitude = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).magnitude + 0.0001f;
        float horizontalResistAdjustment = 1.0f;
        if (currentHorizontalMagnitude > horizontalResistSpeed * 3.0f)
        {
            float speedCap = horizontalMaxSpeed * 3.0f;
            float adjustedSpeed = currentHorizontalMagnitude;
            if (speedCap < currentHorizontalMagnitude)
            {
                adjustedSpeed = speedCap;
            }
            horizontalResistAdjustment = (adjustedSpeed - horizontalResistFactor * (adjustedSpeed - horizontalResistSpeed) * Time.fixedDeltaTime) /
                                    currentHorizontalMagnitude;
        }

        // Apply Verticle Resistance
        float currentVerticalMagnitude = rb.velocity.y + 0.0001f;
        float verticalResistAdjustment = rb.velocity.y;
        if (currentVerticalMagnitude > upResistSpeed)
        {
            float speedCap = upMaxSpeed;
            if (currentVerticalMagnitude > upMaxSpeed) verticalResistAdjustment = upMaxSpeed;
            verticalResistAdjustment = rb.velocity.y - upResistFactor * (rb.velocity.y - upResistSpeed) * Time.fixedDeltaTime;
        }
        if (currentVerticalMagnitude < -downResistSpeed) {
            float speedCap = -downMaxSpeed;
            if (currentVerticalMagnitude < -downMaxSpeed) verticalResistAdjustment = -downMaxSpeed;
            verticalResistAdjustment = downResistFactor * (rb.velocity.y - downResistSpeed) * Time.fixedDeltaTime + rb.velocity.y;
        }

        // Apply Resistance Values
        rb.AddForce(-rb.velocity + new Vector3(rb.velocity.x * horizontalResistAdjustment, verticalResistAdjustment, rb.velocity.z * horizontalResistAdjustment), ForceMode.VelocityChange);


        // Apply Drag
        float chosenDrag = 0.0f;
        if (distanceToGround <= airCushionHeight)
        {
            chosenDrag = airCushionDrag;
        }
        else
        {
            chosenDrag = drag;
        }
        rb.drag = chosenDrag;


        Vector3 accumulatedVelocityChanges = Vector3.zero;

        Vector3 movementDirectionAdjusted = Vector3.ProjectOnPlane(movementDirection, groundNormal).normalized;
        // Debug.Log("Movement Direction: " + movementDirection + " Adjusted: " + movementDirectionAdjusted + " Ground Normal: " + groundNormal + " Is Grounded: " + isGrounded + " Distance to Ground: " + distanceToGround);


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

        // Get speed for given direction (m/s)
        float moveSpeed = 0f;
        if (isMoving)
        {
            if (Mathf.Abs(movement.x) >= Mathf.Abs(movement.z)) moveSpeed = maxRunSideSpeed;
            else moveSpeed = movement.z >= 0 ? maxRunForwardSpeed : maxRunBackwardSpeed;
        }


        if (!isJetting && !isSkiing)
        {
            // Air Control
            if (!isGrounded && isMoving)
            {
                Vector3 airControlAcc = movementDirection * (((moveSpeed / Time.fixedDeltaTime) * rb.mass) / movementDirection.magnitude);
                airControlAcc.y = 0.0f;
                float runSpeed = airControlAcc.magnitude;

                float maxAirControlAcc = runForce * 0.3f;
                
                if (runSpeed > maxAirControlAcc)
                {
                    maxAirControlAcc /= runSpeed;
                    airControlAcc.x *= maxAirControlAcc;
                    airControlAcc.z *= maxAirControlAcc;
                }
                accumulatedVelocityChanges += airControlAcc;
                Debug.Log(airControlAcc);
            }
            // Running
            if (isRunning)
            {
                // Vector3 inputAcc = movementDirectionAdjusted * moveSpeed;
                Vector3 inputAcc = movementDirectionAdjusted * (((moveSpeed / Time.fixedDeltaTime) * rb.mass) / movementDirectionAdjusted.magnitude);

                // inputAcc -= new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);
                float inputSpeed = inputAcc.magnitude;
                float maxAcc = runForce;
                if (inputSpeed > maxAcc) inputAcc *= maxAcc / inputSpeed;
                accumulatedVelocityChanges += inputAcc;
                // TODO: Something to do with gravity?
            }
        }
        else
        {
            // Skating, Jetting
            // Apply Air Cushion and Skating?
            if (isSkiing)
            {
                if (distanceToGround < hoverHeightMax)
                {
                    // Normalized distance between last frame and this frame if it is greater than 1.0f
                    if (Mathf.Abs(lastGroundDistance - distanceToGround) > 1.0f)
                    {
                        lastGroundDistance = distanceToGround + (Mathf.Sign(lastGroundDistance - distanceToGround) * 0.5f);
                    }

                    // Spring force opposing gravity
                    float hoverFactor = hoverStrength * ((hoverHeightMax - distanceToGround)/hoverHeightMax) +
                        (hoverDampening * ((lastGroundDistance - distanceToGround)/hoverHeightMax));
                    hoverFactor = Mathf.Max(hoverFactor, 0.0f);
                    Debug.Log(hoverFactor + " " + distanceToGround + " " + lastGroundDistance + " " + hoverHeightMax + " " + hoverStrength + " " + hoverDampening);

                    float velocityDirection = Vector3.Dot(groundNormal, Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized);

                    Vector3 hoverForce = Vector3.zero;
                    if (velocityDirection > 0.0f)
                    {
                        accumulatedVelocityChanges.x += hoverFactor * 2.0f * Vector3.ProjectOnPlane(groundNormal, Vector3.up).x;
                        accumulatedVelocityChanges.z += hoverFactor * 2.0f * Vector3.ProjectOnPlane(groundNormal, Vector3.up).z;
                        hoverForce = Vector3.up * hoverFactor;
                        // hoverForce = Vector3.up * hoverFactor + Vector3.ProjectOnPlane(groundNormal, Vector3.up) * hoverFactor * 2.0f;
                    }
                    else
                    {
                        Vector3 velocityDirectionNoY = new Vector3(rb.velocity.normalized.x, 0, rb.velocity.normalized.z);
                        Vector3 adjustedGround = groundNormal - velocityDirectionNoY * velocityDirection;
                        float newVelocityDirection = Vector3.Dot(adjustedGround, -rb.velocity.normalized);
                        Vector3 horizontalAdjustment = (adjustedGround - velocityDirectionNoY * newVelocityDirection * -1.0f) * hoverFactor * 0.5f;

                        hoverForce = Vector3.up * hoverFactor;
                        // hoverForce = Vector3.up * hoverFactor + horizontalAdjustment;
                        accumulatedVelocityChanges.x += horizontalAdjustment.x;
                        accumulatedVelocityChanges.z += horizontalAdjustment.z;
                    }
                    
                    
                    rb.AddForce(hoverForce);
                }

                Debug.DrawRay(playerPositionCenter + Vector3.up * 1.2f, Vector3.up * distanceToGround, Color.cyan, 10.0f);
                lastGroundDistance = distanceToGround;
            }
        
            // Jet Air Move
            if (rb.velocity.magnitude < jetAirMoveMaxSpeed && (isSkiing || isJetting))
            {
                float airMoveSpeed = 1.0f;
                if (rb.velocity.magnitude < jetAirMoveMinSpeed)
                {
                    airMoveSpeed = jetAirMoveMaxAccelFactor;
                    if(rb.velocity.magnitude != 0.0f && jetAirMoveMinSpeed / rb.velocity.magnitude <= jetAirMoveMaxAccelFactor)
                    {
                        airMoveSpeed = jetAirMoveMinSpeed / rb.velocity.magnitude;
                    }
                }
                if (isMoving || !isGrounded)
                {
                    float jetDirectionalAcc = horizontalJetForce * airMoveSpeed;
                    accumulatedVelocityChanges.x += jetDirectionalAcc * movementDirection.x;
                    accumulatedVelocityChanges.z += jetDirectionalAcc * movementDirection.z;
                }

                // Jet Up/Down Control
                if (isUpJetting)
                {
                    float airCushionScale = 0.0f;
                    if (airCushionHeight > 0.0f && distanceToGround <= airCushionHeight)
                    {
                        airCushionScale = (airCushionHeight - distanceToGround) / airCushionHeight;
                    }
                    float upAcc = upJetForce * airMoveSpeed;
                    accumulatedVelocityChanges.y += upAcc * airCushionScale * 0.5f + upAcc;
                }
                else if (isDownJetting)
                {
                    accumulatedVelocityChanges.y -= downJetForce * airMoveSpeed;
                }
            }
        }


        // Apply Jetting Verticle Resistance
        float verticalResistanceFactor = 1.0f;
        currentVerticalMagnitude = Vector3.Project(rb.velocity, Vector3.up).magnitude + 0.00001f;
        float currentVerticalAccMagnitude = Vector3.Project(accumulatedVelocityChanges, Vector3.up).magnitude / rb.mass * Time.fixedDeltaTime; // TODO: this may need to also include gravity
        if (currentVerticalMagnitude != 0.0 && currentVerticalAccMagnitude != 0.0)
        {
            float scaledVerticleMagnitude = Mathf.Pow(currentVerticalMagnitude, verticalJetResistanceFactor);
            float verticleResistanceInfluence = scaledVerticleMagnitude * verticalJetResistance;
            if (0.25f < verticleResistanceInfluence)
            {
                verticleResistanceInfluence = 0.25f;
            }
            verticalResistanceFactor = ((currentVerticalMagnitude - verticleResistanceInfluence * currentVerticalAccMagnitude) / currentVerticalMagnitude);
        }

        // Apply Jetting Horizontal Resistance
        float horizontalResistanceFactor = 1.0f;
        currentHorizontalMagnitude = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).magnitude + 0.00001f;
        float currentHorizontalAccMagnitude = Vector3.ProjectOnPlane(accumulatedVelocityChanges, Vector3.up).magnitude / rb.mass * Time.fixedDeltaTime;
        if (currentHorizontalMagnitude != 0.0f && currentHorizontalAccMagnitude != 0.0f)
        {
            float scaledHorizontalMagnitude = Mathf.Pow(currentHorizontalMagnitude, horizontalJetResistanceFactor);
            float horizontalResistanceInfluence = scaledHorizontalMagnitude * horizontalJetResistance;
            if (0.925f < horizontalResistanceInfluence)
            {
                horizontalResistanceInfluence = 0.925f;
            }
            horizontalResistanceFactor = (currentHorizontalMagnitude - horizontalResistanceInfluence * currentHorizontalAccMagnitude) / currentHorizontalMagnitude;
        }

        // Apply Resistance Values
        rb.AddForce(-rb.velocity + new Vector3(rb.velocity.x * horizontalResistanceFactor, rb.velocity.y * verticalResistanceFactor, rb.velocity.z * horizontalResistanceFactor), ForceMode.VelocityChange);

        // Apply Final Acceleration forces
        if (accumulatedVelocityChanges.magnitude > 0.0f)
            rb.AddForce(accumulatedVelocityChanges);
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
