using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlayerMovement : MonoBehaviour
{

    // Assingables
    public Transform playerCam;
    public Transform orientation;

    private Rigidbody rb;

    // Keyboard or Controller
    public bool keyboard = true;

    // Rotation and look
    private float xRotation;
    private float sensitivity = 1f;
    private float desiredX;
    private float wallRunCameraTilt = 0;
    public float maxWallRunCameraTilt = 10;
    public float cameraTiltSpeed = 3;

    // Movement
    public float moveAccel = 50;
    public float maxSpeed = 20;
    public float airborneMovementCoefficent = .1f;
    private float movementCoefficent = 1f;

    // Grounded
    public bool grounded;
    public LayerMask whatIsGround;
    RaycastHit groundHitInfo;

    // Wall
    public bool isWallRunning;
    public LayerMask whatIsWall;
    public float wallrunForce = 1f;
    public float maxWallSpeed = 25f;
    RaycastHit wallHitInfo;
    GameObject hitWallObject;
    GameObject cannotRunOnWallObject;
    public float sameWallCooldown = 1f;
    public float wallRunStickForce = 100;
    public float wallJumpForce = 1000;

    // Slope
    public float maxSlopeAngle = 35f;

    // Jump
    public float jumpForce = 250;
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public bool readyToDoubleJump = true;
    public float horizontalDoubleJumpForce = 200f;

    // Counter Movement
    private float threshold = 0.01f;
    public float counterMovement = 0.175f;
    private float counterCoefficent = 1f;
    public float airborneCounterCoefficent = .1f;

    // Variables
    float leftRightInput, forwardBackwardInput;
    public bool jumpHeld, jumpDown;
    private Vector3 tiltInput, projForward, movementDirection, horizontal;
    public int speedState = 0; // 0 = grounded run, 1 = wallrun, 2 = boost, 3 = air after ground run, 4 = air after wall run, 5 = air after boost
    private int consecutiveIdleFixedUpdates = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        CheckGround();
        SetMovementDirection();
        CheckForWall();

        if (grounded) // May be unnecessary
        {
            speedState = 0;
        }

        Movement();
    }

    private void Update()
    {
        MyInput();
        Look();
    }

    /// <summary>
    /// Collects user inputs
    /// </summary>
    private void MyInput()
    {
        leftRightInput = Input.GetAxisRaw("Horizontal");
        forwardBackwardInput = Input.GetAxisRaw("Vertical");

        // jumpHeld is true while the button is pressed
        jumpHeld = Input.GetButton("Jump");

        // jumpDown is only true the frame that jump is initially pressed, but this extends it to be true until fixedUpdate() is run once
        if (!jumpDown)
        {
            jumpDown = Input.GetButtonDown("Jump");
        }
    }

    /// <summary>
    /// Handles all player movement
    /// </summary>
    private void Movement()
    {
        // Sets velocity to 0 if player velocity is near 0 for 5 fixedUpdates (0.1 sec)
        if (rb.velocity.magnitude != 0 && rb.velocity.magnitude < 0.001)
        {
            consecutiveIdleFixedUpdates++;
            if (consecutiveIdleFixedUpdates > 4)
            {
                rb.velocity = Vector3.zero;
            }
        } else
        {
            consecutiveIdleFixedUpdates = 0;
        }

        // Adds aditional gravity (makes physics work better idk)
        float gravityMultiplier = 10f;
        rb.AddForce(Vector3.down * Time.fixedDeltaTime * gravityMultiplier);

        // Player jumps or double jumps if conditions are met
        if (grounded && !isWallRunning && jumpHeld && readyToJump)
        {
            Jump();
        } else if (!grounded && !isWallRunning && jumpDown && readyToDoubleJump)
        {
            DoubleJump();
        } else if (!grounded && isWallRunning && jumpDown)
        {
            WallJump();
        }

        // Resets jumpDown variable to false
        jumpDown = false;

        // Limits player influence on movement while not grounded
        movementCoefficent = 1f;
        if (!grounded)
        {
            movementCoefficent = airborneMovementCoefficent;
        }

        horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Should be able to set this even earlier if needed

        // Movement force is handled depending on which state the player is currently in (switch to case later)
        if (speedState == 0) // Running on ground
        {
            AccelerateTo(movementDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
        }
        else if (speedState == 1) // Wallrunning
        {
            Wallrun();
            if (horizontal.magnitude > maxWallSpeed)
            {
                horizontal = horizontal.normalized * maxWallSpeed;
                rb.velocity = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
            }
        }
        else if (speedState == 3) // Air after ground run
        {
            AccelerateTo(movementDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
        }
        else if (speedState == 4) // Air after wallrun
        {
            AccelerateTo(movementDirection * maxWallSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
            if (horizontal.magnitude > maxWallSpeed)
            {
                horizontal = horizontal.normalized * maxWallSpeed;
                rb.velocity = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
            }
        }
        else if (horizontal.magnitude > 40) //Unnecessary
        {
            horizontal = horizontal.normalized * 40;
            rb.velocity = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
        }
        //Debug.Log(speedState);

        // Handles player slowdown when there is little or no player directional input (no longer necessary)
        //Vector2 horizontalMagRelative = FindVelRelativeToLook();
        //CounterMovement(horizontalMagRelative);

        //Debug.Log(horizontal.magnitude);
    }

    /// <summary>
    /// Handles acceleration up until desiredVelocity is reached, 
    /// limiting acceleration to either accelerationLimit, or decelerationLimit
    /// </summary>
    /// <param name="desiredVelocity">A movement Vector3 times the maximum allowed velocity</param>
    /// <param name="accelerationLimit">Maximum acceleration the player can make when desired input 
    /// direction is within 90 degrees of current velocity direction</param>
    /// <param name="decelerationLimit">Maximum acceleration the player can make when desired input 
    /// direction is beyond 90 degrees of current velocity direction</param>
    private void AccelerateTo(Vector3 desiredVelocity, float accelerationLimit, float decelerationLimit)
    {
        var deltaV = desiredVelocity - horizontal;
        var acceleration = deltaV / Time.fixedDeltaTime;

        float limit = accelerationLimit;
        if (Vector3.Dot(horizontal, desiredVelocity) <= 0f)
        {
            limit = decelerationLimit;
        }
            
        acceleration = Vector3.ClampMagnitude(acceleration, limit);

        rb.AddForce(acceleration);
    }

    /// <summary>
    /// Sets the direction that the player is attempting to move, adjusted for slopes
    /// </summary>
    private void SetMovementDirection()
    {
        // Creates a directional player input Vector3 that is normalized if using a keyboard
        tiltInput = Vector3.ClampMagnitude(new Vector3(leftRightInput, 0, forwardBackwardInput), 1f);

        // The direction the player is facing adjusted for slopes
        projForward = orientation.forward - (Vector3.Dot(orientation.forward, groundHitInfo.normal) * groundHitInfo.normal);

        // The direction that the player is attempting to move, adjusted for slopes
        movementDirection = Quaternion.LookRotation(projForward, groundHitInfo.normal) * tiltInput;
        movementDirection.Normalize();

        // Negates vertical component when positive so that additional force is not applied upwards
        if (movementDirection.y > 0)
        {
            movementDirection = new Vector3(movementDirection.x, 0, movementDirection.z);
        }
    }

    /// <summary>
    /// Handles player grounded jumps
    /// </summary>
    private void Jump()
    {
        // Resets vertical velocity to 0 (useful for jumping while moving on slopes)
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        // Adds jumpForce amount of force in upward direction
        rb.AddForce(Vector2.up * jumpForce);

        // Player is unable to jump again for jumpCooldown length 
        // (useful so that player does not jump multiple times before exiting ground collision)
        readyToJump = false;
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    /// <summary>
    /// Sets readyToJump to true
    /// </summary>
    private void ResetJump()
    {
        readyToJump = true;
    }

    /// <summary>
    /// Handles player mid air jumps
    /// </summary>
    private void DoubleJump()
    {
        // Resets vertical velocity to 0 if descending
        if (rb.velocity.y < 0)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }

        // Adds force in direction of player movement and upwards
        // (serves to allow player to use double jump to significantly alter horizontal direction midair)
        Vector3 jumpDirection = new Vector3(movementDirection.x, 0, movementDirection.z);

        // Probably a better way to do this
        if (speedState == 3)
        {
            AccelerateTo(jumpDirection * maxSpeed, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
        }
        else if (speedState == 4)
        {
            AccelerateTo(jumpDirection * maxWallSpeed, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
        } else // Should not be here
        {
            Debug.Log("SHOULD NOT BE HERE");
            AccelerateTo(jumpDirection * maxSpeed, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
        }
        rb.AddForce(Vector2.up * jumpForce);

        // Player is unable to double jump again until they touch the ground or a wallrunnable wall
        readyToDoubleJump = false;
    }

    /// <summary>
    /// Checks whether the slope of the param is less than maxSlopeAngle
    /// </summary>
    /// <param name="v">Slope to be checked</param>
    /// <returns>Returns true if slope of the param is less than maxSlopeAngle</returns>
    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    /// <summary>
    /// Sets grounded to true if there is a ground layer object within a certain distance below player RigidBody, false otherwise.
    /// Also resets readyToDoubleJump to true if grounded is set to true
    /// </summary>
    private void CheckGround()
    {
        // Checks for ground layer object 
        if (Physics.Raycast(rb.position, -Vector3.up, out groundHitInfo, 1.15f, whatIsGround))
        {
            grounded = true;
            readyToDoubleJump = true;

            // state management
            speedState = 0;
        }
        else
        {
            grounded = false;

            // state management
            if (speedState < 3 && !isWallRunning)
            {
                speedState = 3;
            }
        }
    }

    /// <summary>
    /// Handles player camera rotation
    /// </summary>
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        //Find current look rotation
        Vector3 rot = playerCam.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Tilts camera while wallrunning
        if (isWallRunning) {

            float dot = Vector3.Dot(Vector3.Cross(orientation.forward, wallHitInfo.normal), new Vector3(1, 1, 1));
            
            if (wallRunCameraTilt < maxWallRunCameraTilt && dot < 0)
            {
                wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;

                // Limits wallRunCameraTilt to exactly limit
                if (wallRunCameraTilt > maxWallRunCameraTilt)
                    wallRunCameraTilt = maxWallRunCameraTilt;

            } else if (wallRunCameraTilt > -maxWallRunCameraTilt && dot > 0)
            {
                wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;

                // Limits wallRunCameraTilt to exactly limit
                if (wallRunCameraTilt < -maxWallRunCameraTilt)
                    wallRunCameraTilt = -maxWallRunCameraTilt;
            }
        }

        // Tilts camera back again
        if (wallRunCameraTilt > 0 && !isWallRunning)
        {
            if (wallRunCameraTilt <= .5) // At 60fps, max camera tilt error is .5, at 30fps its 1.0 with maxtilt 10 and tiltspeed 3
            {
                wallRunCameraTilt = 0;
            } else
            {
                wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;
            }
        } else if (wallRunCameraTilt < 0 && !isWallRunning)
        {
            if (wallRunCameraTilt >= -.5) // At 60fps, max camera tilt error is .5, at 30fps its 1.0 with maxtilt 10 and tiltspeed 3
            {
                wallRunCameraTilt = 0;
            } else
            {
                wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;
            }
        }

        //Perform the rotations
        playerCam.localRotation = Quaternion.Euler(xRotation, desiredX, wallRunCameraTilt);
        orientation.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    /// <summary>
    /// Handles player slowdown when there is little or no player directional input
    /// </summary>
    /// <param name="mag">Vector2 containing directional movement relative to player look direction</param>
    private void CounterMovement(Vector2 mag)
    {
        // Used to slow the player much less if they are not grounded
        if (grounded) 
        {
            counterCoefficent = 1f;
        } else
        {
            counterCoefficent = airborneCounterCoefficent;
        }

        if (Math.Abs(mag.x) > threshold && Math.Abs(leftRightInput) < 0.05f || (mag.x < -threshold && leftRightInput > 0) || (mag.x > threshold && leftRightInput < 0))
        {
            rb.AddForce(moveAccel * orientation.right * Time.fixedDeltaTime * -mag.x * counterMovement * counterCoefficent);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(forwardBackwardInput) < 0.05f || (mag.y < -threshold && forwardBackwardInput > 0) || (mag.y > threshold && forwardBackwardInput < 0))
        {
            rb.AddForce(moveAccel * orientation.forward * Time.fixedDeltaTime * -mag.y * counterMovement * counterCoefficent);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = (float)Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2));

        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    /// <summary>
    /// Handles movement force if player is wallrunning
    /// </summary>
    private void Wallrun()
    {
        // wallrunForce = 500, wallRunStickForce = 100, moveAccel = 50, maxWallSpeed = 30

        // Turns gravity off if the player is not ascending
        if (rb.velocity.y > 0.5f)
        {
            rb.useGravity = true;
        } else
        {
            rb.useGravity = false;
            rb.AddForce(-Vector3.up * 10);
        }

        Vector3 maxDirectionalSpeed = new Vector3(Math.Min(Math.Max(movementDirection.x * 2, -1), 1), 0, Math.Min(Math.Max(movementDirection.z * 2, -1), 1));
        AccelerateTo(maxDirectionalSpeed * maxWallSpeed, wallrunForce, moveAccel);

        // Force to help player stick to wall
        rb.AddForce(-wallHitInfo.normal * wallRunStickForce); //This will not work for walls that are angled (probably just needs y component = 0)
    }

    /// <summary>
    /// Checks whether the player is moving into or perpendicular to a wall and sets isWallRunning accordingly
    /// </summary>
    private void CheckForWall()
    {
        // This is useful for determing the direction the player is trying to move along the wall (not used for anything atm)
        // dot = Vector3.Dot(Vector3.Cross(rayDirection, wallHitInfo.normal), new Vector3(1, 1, 1));

        if (!isWallRunning && !grounded)
        {
            if (Physics.Raycast(rb.position, movementDirection, out wallHitInfo, 1f, whatIsWall))
            {
                if (cannotRunOnWallObject == null || cannotRunOnWallObject.GetInstanceID() != wallHitInfo.collider.gameObject.GetInstanceID())
                {
                    hitWallObject = wallHitInfo.collider.gameObject;

                    // Resets vertical velocity to 0 if descending when first colliding with wall
                    if (rb.velocity.y < 0)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    }

                    isWallRunning = true;
                    readyToDoubleJump = true;

                    // state management
                    speedState = 1;
                }
            }
            
        } else if (isWallRunning)
        {
            if (!Physics.Raycast(rb.position, -wallHitInfo.normal, out wallHitInfo, 1f, whatIsWall))
            {
                Invoke(nameof(StopWallRun), .2f);
            } else if (Vector3.Angle(movementDirection, -wallHitInfo.normal) > 120f) 
            {
                Invoke(nameof(StopWallRun), .2f);
            } else if (grounded)
            {
                StopWallRun();
            }
        }
    }

    /// <summary>
    /// Sets isWallRunning to false, turns gravity back on, and does not allow for the same wall to be wallrun again for sameWallCooldown
    /// </summary>
    private void StopWallRun()
    {
        isWallRunning = false;
        rb.useGravity = true;

        // Stores the wall that was just wallrun on so that it cannot be run on again
        cannotRunOnWallObject = hitWallObject;

        // Resets the wall to be wallrunnable again after sameWallCooldown
        Invoke(nameof(resetCannotRunOnWallObject), sameWallCooldown);

        // state management
        speedState = 4;
    }

    /// <summary>
    /// Handles jumping off walls
    /// </summary>
    private void WallJump()
    {
        // Resets vertical velocity to 0 if descending
        if (rb.velocity.y < 0)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }

        Vector3 jumpDirection = new Vector3((movementDirection.x/2 + wallHitInfo.normal.x), 1, (movementDirection.z/2 + wallHitInfo.normal.z)); // Can be seperated from vertical component to better control horizontal push off wall
        rb.AddForce(jumpDirection * jumpForce);
        StopWallRun();
    }

    /// <summary>
    /// Sets cannotRunWallObject to null
    /// </summary>
    private void resetCannotRunOnWallObject()
    {
        cannotRunOnWallObject = null;
    }

}
