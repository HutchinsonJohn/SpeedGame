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
    public bool isWallLeft;
    public bool isWallRight;
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
    private Vector3 tiltInput, projForward, rayDirection, horizontal;
    public int speedState = 0; // 0 = grounded run, 1 = wallrun, 2 = boost, 3 = air after ground run, 4 = air after wall run, 5 = air after boost

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
        // Adds aditional gravity (makes physics work better idk)
        float gravityMultiplier = 50f;
        rb.AddForce(Vector3.down * Time.fixedDeltaTime * gravityMultiplier);

        // Creates a directional player input Vector3 that is normalized if using a keyboard
        tiltInput = Vector3.ClampMagnitude(new Vector3(leftRightInput, 0, forwardBackwardInput), 1f);

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
        
        // The direction the player is facing adjusted for slopes
        projForward = orientation.forward - (Vector3.Dot(orientation.forward, groundHitInfo.normal) * groundHitInfo.normal);
        

        // The direction that the player is attempting to move, adjusted for slopes
        rayDirection = Quaternion.LookRotation(projForward, groundHitInfo.normal) * tiltInput;
        rayDirection.Normalize();

        // Negates vertical component when positive so that additional force is not applied upwards
        if (rayDirection.y > 0)
        {
            rayDirection = new Vector3(rayDirection.x, 0, rayDirection.z);
        }

        horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Should be able to set this even earlier if needed

        // Movement force is handled depending on which state the player is currently in (switch to case later)
        if (speedState == 0) // Running on ground
        {
            AccelerateTo(rayDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
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
            AccelerateTo(rayDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
        }
        else if (speedState == 4) // Air after wallrun
        {
            AccelerateTo(rayDirection * maxWallSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
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

        Debug.Log(horizontal.magnitude);
    }

    private void AccelerateTo(Vector3 desiredVelocity, float accelerationLimit, float decelerationLimit)
    {
        var deltaV = desiredVelocity - horizontal;
        var acceleration = deltaV / Time.fixedDeltaTime;

        float limit = accelerationLimit;
        if (Vector3.Dot(horizontal, desiredVelocity) <= 0f)
            limit = decelerationLimit;
        acceleration = Vector3.ClampMagnitude(acceleration, limit);

        rb.AddForce(acceleration);
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
        Vector3 jumpDirection = new Vector3(rayDirection.x, 0, rayDirection.z);
        AccelerateTo(jumpDirection * maxSpeed, horizontalDoubleJumpForce, new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
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

        //Perform the rotations
        playerCam.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
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
        // Turns gravity off if the player is not ascending
        if (rb.velocity.y > 0.5f)
        {
            rb.useGravity = true;
        } else
        {
            rb.useGravity = false;
        }

        // Large force in player movement direction, usually directed into the wall at an angle
        rb.AddForce(rayDirection * wallrunForce * Time.fixedDeltaTime);

        // Force to help player stick to wall
        rb.AddForce(-wallHitInfo.normal * wallRunStickForce); //This will not work for walls that are angled (probably just needs y component = 0)
    }
    
    /// <summary>
    /// Checks whether the player is moving into or perpendicular to a wall and sets isWallRunning accordingly
    /// </summary>
    private void CheckForWall()
    {
        // This is useful for determing the direction the player is trying to move along the wall (not used for anything atm)
        //dot = Vector3.Dot(Vector3.Cross(rayDirection, wallHitInfo.normal), new Vector3(1, 1, 1));

        if (!isWallRunning && !grounded)
        {
            if (Physics.Raycast(rb.position, rayDirection, out wallHitInfo, 1f, whatIsWall) || Physics.Raycast(orientation.right, rayDirection, out wallHitInfo, 1f, whatIsWall))
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
            
        } else if (isWallRunning) // This could be one big else if A || B || C || D
        {
            if (!Physics.Raycast(rb.position, -wallHitInfo.normal, out wallHitInfo, 1f, whatIsWall))
            {
                Invoke(nameof(StopWallRun), .1f);
            } else if (Vector3.Angle(rayDirection, -wallHitInfo.normal) > 120f) 
            {
                Invoke(nameof(StopWallRun), .1f);
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
        Vector3 jumpDirection = new Vector3((rayDirection.x/2 + wallHitInfo.normal.x), 1, (rayDirection.z/2 + wallHitInfo.normal.z));
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
