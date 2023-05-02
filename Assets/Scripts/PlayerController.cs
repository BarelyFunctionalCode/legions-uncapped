using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float drag = 0.004f;                         //<----- Drag for running or jetting above 10 meters
    private float airCushionDrag = 0.00275f;             //<----- this is probably what the rigidbody drag should be when in the air between 0-10 meters from the ground
    private float airCushionHeight = 10.0f;              //<----- this is probably the height at which the airCushionDrag stops being applied

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

    private float jetSkateMinSpeed = 10;
    private float jetSkateMaxSpeed = 1000;
    private float jetSkateMaxAccelFactor = 3;


    // Air control
    // private bool useDirectionalAirControl = false;        // uhhhhhh
    private float directionalAirControlK = 32f; 
    private float directionalAirControlForce = 275f;
    
    // air control
    // private float airControl = 1.0f;

    private float mass = 75f;

    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float rotationLimit = 60f;

    [SerializeField] private PhysicMaterial skiMaterial;
    [SerializeField] private PhysicMaterial normalMaterial;

    private PlayerControls playerControls;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

    private Vector3 lastVelocity = Vector3.zero;
    private Vector3 lastPosition = Vector3.zero;
    private Vector3 lastKnownGroundedPoint = Vector3.zero;
    private List<ContactPoint> contactPoints = new List<ContactPoint>();

    private float maxStepHeight = 0.4f;        // The maximum a player can set upwards in units when they hit a wall that's potentially a step
    private float stepSearchOvershoot = 0.005f; // How much to overshoot into the direction a potential step in units when testing. High values prevent player from walking up tiny steps but may cause problems.


    void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        playerCollider = GetComponent<CapsuleCollider>();
        playerCollider.material = normalMaterial;
    }

    void OnApplicationFocus(bool hasFocus) { if (hasFocus) Cursor.lockState = CursorLockMode.Locked; }
    void OnEnable() { playerControls.Enable(); }
    void OnDisable() { playerControls.Disable(); }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        contactPoints.Clear();
        lastVelocity = rb.velocity;
        lastPosition = transform.position;
    }

    private void HandleMovement()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.MoveVector.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        // Get direction of movement relative to player rotation
        Vector3 movementDirection = transform.TransformDirection(movement).normalized;
    
        // Get input for skiing, jumping, and down jetting
        bool isMoving = movement.magnitude > 0.0f;
        bool isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f;
        bool isJumping = playerControls.Movement.Jump.ReadValue<float>() > 0.0f;
        bool isJetting = isSkiing && isJumping;
        bool isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;

        // Set friction based on whether player is skiing or not
        playerCollider.material = isSkiing ? skiMaterial : normalMaterial;

        Vector3 currentVelocity = rb.velocity;
        Vector3 currentVelocityLocalSpace = transform.InverseTransformDirection(currentVelocity);

        // Get normal of ground below player
        // TODO: Use Contact Points to get ground data
        RaycastHit hit;
        bool didHit = Physics.Raycast(new Ray(transform.position, Vector3.down), out hit, airCushionHeight);
        Vector3 groundNormal = didHit ? hit.normal : Vector3.up;
        Vector3 groundPoint = didHit ? hit.point : Vector3.zero;
        float distanceToGround = didHit ? hit.distance : airCushionHeight;
        bool isGrounded = contactPoints.Count > 0 || (didHit && hit.distance <= (isSkiing ? 3.3f : 1.3f));

        // Check if the player runs into a step
        // if (isGrounded)
        // {
        //     Vector3 possibleGroundPoint = groundPoint;
        //     foreach (ContactPoint cp in contactPoints)
        //     {
        //         Vector3? stepUpOffset = ResolveStepUp(cp, groundPoint);
        //         if (stepUpOffset != null)
        //         {
        //             Debug.Log("Step Up: " + (Vector3)stepUpOffset);
        //             possibleGroundPoint = transform.position + (Vector3)stepUpOffset;
        //             rb.position += (Vector3)stepUpOffset;
        //             rb.velocity = lastVelocity;
        //             break;
        //         }
        //     }
        //     lastKnownGroundedPoint = possibleGroundPoint;
        // }
        // else
        // {
        //     // Walk on ledge forgiveness (if player is not grounded but is close to the ground, they are considered grounded)
        //     if (lastKnownGroundedPoint != Vector3.zero)
        //     {
        //         Debug.Log("Distance to last known ground:" + (Vector3.Distance(
        //             transform.position - (transform.up * (playerCollider.height / 2f)), 
        //             lastKnownGroundedPoint
        //         )) + " < " + (playerCollider.radius));
        //         if (Vector3.Distance(
        //             transform.position - (transform.up * (playerCollider.height / 2f)), 
        //             lastKnownGroundedPoint
        //         ) < playerCollider.radius)
        //         {
        //             Debug.Log("Should adjust position? " + (transform.position.y < lastPosition.y));
        //             if (transform.position.y < lastPosition.y)
        //             {
        //                 rb.position = new Vector3(
        //                     transform.position.x,
        //                     lastPosition.y,
        //                     transform.position.z
        //                 );
        //             }
        //             isGrounded = true;
        //         }
        //         else
        //         {
        //             lastKnownGroundedPoint = Vector3.zero;
        //         }
        //     }
        // }

        // Calculate drag based on distance to ground
        float adjustedDrag = Mathf.Lerp(
            airCushionDrag,
            drag,
            distanceToGround / airCushionHeight
        );
        rb.drag = adjustedDrag;

        // Return if the player isn't moving
        if (!isMoving && !isSkiing) return;

        movementDirection = Vector3.ProjectOnPlane(movementDirection, groundNormal).normalized;

        Vector3 inputForce = Vector3.zero;
        if (isSkiing)
        {
            // Skiing/Jetting Movement

            // Caclulate horizontal vector
            float currentHorizontalMagnitude = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).magnitude;
            float currentVerticleMagnitude = Vector3.Project(currentVelocity, Vector3.up).magnitude;

            // Base forces for different directions
            Vector3 horizontalVector = movementDirection * horizontalJetForce;
            Vector3 upJetVector = isJetting ? Vector3.up * upJetForce : Vector3.zero;
            Vector3 downJetVector = isDownJetting ? -Vector3.up * downJetForce : Vector3.zero;

            // Scale acceleration based on current speed
            if (!isGrounded)
            {
                float accelFactor = Mathf.Lerp(
                    jetAirMoveMaxAccelFactor,
                    0f,
                    (currentVelocity.magnitude - jetAirMoveMinSpeed) / jetAirMoveMaxSpeed
                );
                horizontalVector *= accelFactor;
                upJetVector *= accelFactor;
                downJetVector *= accelFactor;
            }
            else
            {
                float accelFactor = Mathf.Lerp(
                    jetSkateMaxAccelFactor,
                    0f,
                    (inputForce.magnitude - jetSkateMinSpeed) / jetSkateMaxSpeed
                );
                horizontalVector *= accelFactor;
            }


            // Scale down speed based on resistance speeds
            float horizontalMultiplier = ApplyComponentSpeedLimit(currentHorizontalMagnitude, horizontalResistSpeed, horizontalResistFactor, horizontalMaxSpeed);
            float upMultiplier = ApplyComponentSpeedLimit(currentVerticleMagnitude, upResistSpeed, upResistFactor, upMaxSpeed);
            float downMultiplier = ApplyComponentSpeedLimit(currentVerticleMagnitude, downResistSpeed, downResistFactor, downMaxSpeed);
            horizontalVector *= horizontalMultiplier;
            upJetVector *= upMultiplier;
            downJetVector *= downMultiplier;
            
            // Combine vectors
            inputForce = horizontalVector + upJetVector + downJetVector;
        }
        else if (isGrounded)
        {
            // Running Movement

            // Check the angle of the surface to see if it can be run on
            float angle = Vector3.Angle(Vector3.up, movementDirection);
            bool canMove = 90 - angle < maxRunUpSurfaceAngle;

            // Slope is too steep to run up
            if (!canMove) return;

            // Caclulate velocity change vector
            inputForce = movementDirection * runForce;

            // Cap max speed
            float speedCap = 0f;
            if (Mathf.Abs(currentVelocityLocalSpace.x) > Mathf.Abs(currentVelocityLocalSpace.z)) speedCap = maxRunSideSpeed;
            else speedCap = currentVelocityLocalSpace.z > 0 ? maxRunForwardSpeed : maxRunBackwardSpeed;

            if (currentVelocity.magnitude > speedCap) inputForce = inputForce.normalized * speedCap;
        }
        // else
        // {
        //     Debug.Log("I shouldn't be here... Grounded:" + isGrounded + " Skiing:" + isSkiing);
        // }
        // Air Control
        // if (!isGrounded)
        // {
        //     // Debug.Log("Air Control Impulse: " + (movementDirection * directionalAirControlForce) / directionalAirControlK + "");
        //     inputForce += ((movementDirection * directionalAirControlForce) / directionalAirControlK) * rb.mass;
        //     // rb.AddForce(airControlVelocity - Vector3.ProjectOnPlane(currentVelocity, Vector3.up), ForceMode.Acceleration);
        // }


        
        // Apply movement
        inputForce -= currentVelocity;
        rb.AddForce(inputForce);

    }

    // private Vector3? ResolveStepUp(ContactPoint stepTestCP, Vector3 groundPoint)
    // {
    //     // Check if contact point it potentially a step
    //     if (Mathf.Abs(stepTestCP.normal.y) >= 0.01f) return null;
    //     if (stepTestCP.point.y - groundPoint.y >= maxStepHeight) return null;
        
    //     // Use raycast to check height and depth of step
    //     RaycastHit hitInfo;
    //     Collider stepCol = stepTestCP.otherCollider;
    //     float stepHeight = groundPoint.y + maxStepHeight;
    //     Vector3 stepTestInvDir = new Vector3(-stepTestCP.normal.x, 0, -stepTestCP.normal.z).normalized;
    //     Vector3 origin = new Vector3(stepTestCP.point.x, stepHeight, stepTestCP.point.z) + 
    //                                 (stepTestInvDir * stepSearchOvershoot);
    //     if (!stepCol.Raycast(new Ray(origin, Vector3.down), out hitInfo, maxStepHeight)) return null;
        
    //     // Valid step, calculate offset to move player up
    //     Vector3 stepUpPoint = new Vector3(stepTestCP.point.x, hitInfo.point.y, stepTestCP.point.z) + (stepTestInvDir * stepSearchOvershoot);
    //     return stepUpPoint - new Vector3(stepTestCP.point.x, groundPoint.y, stepTestCP.point.z);
    // }

    private float ApplyComponentSpeedLimit(float currentComponentMagnitude, float resistSpeed, float resistFactor, float maxSpeed)
    {
        float adjustedResistFactor = 1f;

        // Apply resistances
        if (currentComponentMagnitude > resistSpeed)
        {
            adjustedResistFactor = Mathf.Lerp(
                resistFactor,
                0f,
                (currentComponentMagnitude - resistSpeed) / (maxSpeed - resistSpeed)
            );
        }
        return adjustedResistFactor;
    }

    private void HandleRotation()
    {
        Vector2 rotationInput = playerControls.Movement.LookVector.ReadValue<Vector2>();
        Vector3 rotation = new Vector3(0f, rotationInput.x, 0f);
        rotation *= rotationSpeed * Time.fixedDeltaTime;
        rotation = Vector3.ClampMagnitude(rotation, rotationLimit);
        transform.Rotate(rotation);
    }

    void OnCollisionEnter(Collision col)
    {
        contactPoints.AddRange(col.contacts);
    }
    void OnCollisionStay(Collision col)
    {
        contactPoints.AddRange(col.contacts);
    }
}
