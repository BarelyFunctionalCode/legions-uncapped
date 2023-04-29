using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float runForce = 10000f;
    private float maxRunForwardSpeed = 20f;
    private float maxRunBackwardSpeed = 20f;
    private float maxRunSideSpeed = 20f;
    private float maxRunUpSurfaceAngle = 50f; 

    private float horizontalJetForce = 3125f;
    private float horizontalJetResistFactor = 0.3f;
    private float horizontalJetResistSpeed = 100f;              // <---- I think this means that "at this speed, the player will be slowed down by the horizResistFactor"
    private float horizontalJetMaxSpeed = 120f;

    private float upJetForce = 7031.25f;
    private float upJetResistFactor = 0.3f;
    private float upJetResistSpeed = 85f;                  // <---- Same as above
    private float upJetMaxSpeed = 115f;

    private float downJetForce = 5156.25f;
    private float downJetResistFactor = 0.1f;
    private float downJetResistSpeed = 1000f;
    private float downJetMaxSpeed = 1000f;                  //<---- Max speeds for each direction (velocity magnitude)

    private float mass = 75f;

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
    }

    private void HandleMovement()
    {
        // Get movement input
        Vector2 movementInput = playerControls.Movement.MoveVector.ReadValue<Vector2>();
        Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
        bool isMoving = movement.magnitude > 0.0f;
        bool isSkiing = playerControls.Movement.Ski.ReadValue<float>() > 0.0f;
        bool isJumping = playerControls.Movement.Jump.ReadValue<float>() > 0.0f;
        bool isJetting = isSkiing && isJumping;
        bool isDownJetting = playerControls.Movement.DownJet.ReadValue<float>() > 0.0f && isSkiing;

        // Return if the player isn't moving
        if (!isMoving && !isSkiing) return;

        // Get normal of ground below player
        RaycastHit hit;
        bool didHit = Physics.Raycast(new Ray(transform.position + (Vector3.down * (playerCollider.height/2f)), Vector3.down), out hit, 2f);
        Vector3 groundNormal = didHit ? hit.normal : Vector3.up;
        bool isGrounded = didHit && hit.distance < 1f;

        // Get direction of movement relative to player rotation
        Vector3 movementDirection = transform.TransformDirection(movement).normalized;
        movementDirection = Vector3.ProjectOnPlane(movementDirection, groundNormal).normalized;
        Vector3 currentVelocity = rb.velocity;
        Vector3 currentVelocityLocalSpace = transform.InverseTransformDirection(currentVelocity);

        if (isSkiing)
        {
            // Skiing/Jetting Movement

            // Set Zero Friction Material
            playerCollider.material = skiMaterial;

            // Caclulate horizontal vector and apply velocity limits
            float currentVelocityHorizontalMagnitude = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).magnitude;
            Vector3 horizontalVector = ApplyJetVelocityComponentLimit(movementDirection * horizontalJetForce, currentVelocityHorizontalMagnitude,
                                                            horizontalJetResistSpeed, horizontalJetResistFactor, horizontalJetMaxSpeed);

            // Calculate vertical vectors and apply velocity limits
            float currentVelocityUpMagnitude = Vector3.Project(currentVelocity, Vector3.up).magnitude;
            float currentVelocityDownMagnitude = Vector3.Project(currentVelocity, -Vector3.up).magnitude;
            Vector3 upJetVector = ApplyJetVelocityComponentLimit(isJetting ? Vector3.up * upJetForce : Vector3.zero,
                                                    currentVelocityUpMagnitude, upJetResistSpeed, upJetResistFactor, upJetMaxSpeed);
            Vector3 downJetVector = ApplyJetVelocityComponentLimit(isDownJetting ? -Vector3.up * downJetForce : Vector3.zero,
                                                    currentVelocityDownMagnitude, downJetResistSpeed, downJetResistFactor, downJetMaxSpeed);
            
            // Combine vectors
            Vector3 velocityChange = horizontalVector + upJetVector + downJetVector;

            // Apply movement
            rb.AddForce(velocityChange);
        }
        else if (isGrounded)
        {
            // Running Movement

            // Set Normal Friction Material
            playerCollider.material = normalMaterial;

            // Check the angle of the surface to see if it can be run on
            float angle = Vector3.Angle(Vector3.up, movementDirection);
            bool canMove = 90 - angle < maxRunUpSurfaceAngle;

            // Slope is too steep to run up
            if (!canMove) return;

            // Caclulate velocity change vector
            Vector3 velocityChange = movementDirection * runForce;

            // Cap max speed
            float speedCap = 0f;
            if (Mathf.Abs(currentVelocityLocalSpace.x) > Mathf.Abs(currentVelocityLocalSpace.z)) speedCap = maxRunSideSpeed;
            else speedCap = currentVelocityLocalSpace.z > 0 ? maxRunForwardSpeed : maxRunBackwardSpeed;

            if (currentVelocity.magnitude > speedCap) velocityChange = Vector3.zero;

            // Apply movement
            rb.AddForce(velocityChange);
        }
    }

    private Vector3 ApplyJetVelocityComponentLimit(
                                        Vector3 desiredVelocity,
                                        float currentVelocityComponentMagnitude,
                                        float componentResistSpeed,
                                        float componentResistFactor,
                                        float componentMaxSpeed
    )
    {
        // Apply resistances
        if (currentVelocityComponentMagnitude > componentResistSpeed)
        {
            float resistFactor = Mathf.Lerp(
                1f,
                componentResistFactor,
                (currentVelocityComponentMagnitude - componentResistSpeed) / (componentMaxSpeed - componentResistSpeed)
            );
            desiredVelocity *= resistFactor;
        }
        // Cap max speed
        if (currentVelocityComponentMagnitude > componentMaxSpeed) desiredVelocity = Vector3.zero;
        return desiredVelocity;
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

    /* IMPORTANT VALUES

    BASE PLAYER:

    drag = 0.004;                         <----- Drag for running or jetting above 10 meters
    airCushionDrag = 0.00275;             <----- this is probably what the rigidbody drag should be when in the air between 0-10 meters from the ground
    airCushionHeight = 10.0;              <----- this is probably the height at which the airCushionDrag stops being applied

    // jet acceleration resistance        <----- No idea what these values are
    horizontalJetResistance = 0.0017;
    horizontalJetResistanceFactor = 1.8;
    verticalJetResistance = 0.0006;
    verticalJetResistanceFactor = 1.8;
    overdriveJetResistance = 0.00012;
    overdriveJetResistanceFactor = 1.95;

    // directional jetting in air
    // all air movement(up, down, directional)
    jetAirMoveMinSpeed = 5;
    jetAirMoveMaxSpeed = 1000;
    jetAirMoveMaxAccelFactor = 1.5;      <---- ????

    // Jet-Skating
    // cardinal directional
    jetSkateMinSpeed = 10;
    jetSkateMaxSpeed = 1000;
    jetSkateMaxAccelFactor = 3;         <---- ????

    // Air control
    useDirectionalAirControl = false;        // uhhhhhh
    directionalAirControlK = 32; 
    directionalAirControlForce = 275;
    
    // air control
    airControl = 1.0;








    // camera
    cameraMaxDist = 4.5;          // third person camera distance
    cameraMinDist = 0.2;          // closest the camera can be in third person
    cameraDefaultFov = 110.0;     // field of view for cameras on this player (degrees)
    cameraMinFov = 5.0;           // minimum field of view for cameras on this player (degrees)
    cameraMaxFov = 140.0;         // maximum field of view for cameras on this player (degrees)
    minLookAngle = -1.57;         // lowest look angle (radians)
    maxLookAngle = 1.57;          // highest look angle (radians)
    maxFreelookAngle = 0;         // field of view angle when free looking (radians)

    takesFallingDamage = true; // this is a dynamic field
    fallDamageScale = 0.55; //0.53;
    fallDamageCurve = 2.7; //2.65; 
    fallDamageZBias = 0.6; //0.7;
    fallDamageMinSpeed = 50;
    fallDamageBBMultiplier = 0.8;

    // healing
    autohealRate = 6.25;
    autohealGroundHeight = 0.4;
    autohealDamageDelay = 5000;

    // cooling
    coolingRate = 10;
    velocityCooling = 0.25;

    // impact camera shake
    groundImpactMinSpeed = 20;
    groundImpactShakeFreq = "100 100 100";
    groundImpactShakeAmp = "0.075 0.081 0.08";
    groundImpactShakeDuration = 0.2;
    groundImpactShakeFalloff = 2;

    // impact recovery
    minImpactSpeed = 20;
    recoverDelay = 5;

    // jumping
    jumpForce = 2000;
    jumpSurfaceAngle = 80;
    jumpDelay = 0;
    minJumpSpeed = 10;
    maxJumpSpeed = 15;

    // energy
    maxEnergy = 40;               // amount of energy
    rechargeRate = 6.875;           // energy recharge rate
    groundRechargeRate = 12.5;      // on-ground recharge rate (ADDITIVE WITH RECHARGERATE!!)
    jetUpEnergyDrain = 22.5;
    jetDownEnergyDrain = 21.875;
    jetSkateEnergyDrain = 4.6875;

    // Overdrive
    overdriveSpeedZBias = 0.3;
    overdriveEnterSpeed = 88.0;
    overdriveExitSpeed = 78;
    overdriveInitialForceDuration = 2000.0;
    overdriveInitialForce = 220.0;
    overdriveForce = 160.0;
    overdriveDamage = 18.75;
    overdriveEnergyDrain = 1000.0;
    overdriveIsContagious = true;
    overdriveForceRadius = 10;
    overdriveForceScale = 1;
    overdriveForceScaleAhead = 1;
    overdriveForceScaleBehind = 1;
    overdriveForceScaleAlong = 1;
    overdriveForceScaleAgainst = 1;
    overdriveFOV = 25.0;
    heatShellDataBlock = PlayerHeatShell;
    overdriveExplosionDamage = 30;
    overdriveExplosionRadius = 15;
    overdriveExplosionInnerRadius = 5;
    areaImpulse = 0;

    // spawning
    respawnTimeout = 4000;
    suicideRespawnTimeout = 4000;

    // bounds
    boundingBox = "2.1 2.1 2.8";
    boxOffset = "0.0 0.0 0.0";
    pickupRadius = 0.75;




    OUTRIDER:
    maxForwardSpeed = 20;
    maxBackwardSpeed = 20;
    maxSideSpeed = 20;
    dodgeImpulse = 15000;

    // overdrive resistance
    overdriveJetResistance = 0.00008;
    overdriveJetResistanceFactor = 1.95;

    // gameplay changes for Outrider
    mass = 75;
    maxDamage = 75; //70;
    overdriveDamage = 15.625;
    overdriveKillsEnergy = true;
    jetUpForceZ = 7031.25;
    jetDownForceZ = 5156.25;

    fallDamageBBMultiplier = 0.7;

    // lowered max energy
    maxEnergy = 33;
    rechargeRate = 5.625;
    groundRechargeRate = 12.5;

    */