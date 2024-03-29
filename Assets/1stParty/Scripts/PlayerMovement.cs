﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{

    // Assingables
    public Transform playerCam;
    public Transform orientation;

    public Image boostFill;
    public Image boostIcon;
    public Text speedDebug;

    public Color boostReady;
    public Color boostNotReady;

    private Rigidbody rb;

    // Keyboard or Controller
    public bool keyboard = true; // currently unused

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
    private float sameWallCooldown = 1f;
    public float wallRunStickForce = 100;
    public float wallJumpForce = 1000;

    // Boost
    private float maxBoostSpeed = 20f;
    public bool isBoosting;
    private float boostAccel = 1000;
    public bool readyToBoost;
    private int boostMeterLimit = 100; // 50 pts a second (1/fixedDeltaTime)
    private int boostMeterVal = 100;  
    private bool rechargeBoost = true;
    private float boostCooldown = 1f;
    private int rechargeRate = 2;

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
    private float leftRightInput, forwardBackwardInput;
    public bool jumpHeld, jumpDown, boostHeld, boostDown;
    private Vector3 tiltInput, projForward, movementDirection, horizontal;
    /// <summary>
    /// 0 = grounded run, 1 = wallrun, 2 = boost, 3 = air after ground run, 4 = air after wall run or boost
    /// </summary>
    public int speedState = 0;
    private int consecutiveIdleFixedUpdates = 0;

    // Health
    public bool isDying;
    private int health = 5;
    private float regenCooldown = 0;

    // UI
    public GameObject gameOverCanvas;
    public Image healthMeter;
    public TMP_Text timeDisplay;
    public GameObject threeText;
    public GameObject twoText;
    public GameObject oneText;

    // Shooting
    private Transform mainCamera;
    private float shootCooldown;
    public AudioSource akShot;
    public float bulletSize = 0.1f;
    public LayerMask enemy;

    // Swinging
    private bool swinging;
    private float timeSwinging;

    public Animator swordAnimator;
    public Animator gunAnimator;

    // Level
    public bool starting = true;
    public bool tutorial;
    private float levelTime;
    private Coroutine readyGoCoroutine;

    private int killedEnemies;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainCamera = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (starting)
        {
            return;
        }

        if (PauseMenu.GameIsPaused || isDying)
        {
            return;
        }

        if (endReached)
        {
            return;
        }

        CheckGround();
        SetMovementDirection();
        CheckForWall();

        if (grounded) // May be unnecessary
        {
            if (speedState != 2)
            {
                speedState = 0;
            }
        }

        Movement();
    }

    private void Update()
    {
        if (starting)
        {
            if (readyGoCoroutine == null)
            {
                readyGoCoroutine = StartCoroutine(ReadyGoCoroutine());
            }
            return;
        }

        if (PauseMenu.GameIsPaused || isDying)
        {
            return;
        }

        if (endReached)
        {
            return;
        } else
        {
            levelTime += Time.deltaTime;
            timeDisplay.text = FormatTime(levelTime);
        }

        if (health < 5)
        {
            if (regenCooldown <= 0f)
            {
                health++;
                healthMeter.fillAmount = 0.25f + health * 0.15f;
                regenCooldown = .5f;
            }
            else
            {
                regenCooldown -= Time.deltaTime;
            }
        }

        MyInput();
        Look();

        if (shootCooldown > 0)
        {
            shootCooldown -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        } else if (Input.GetButtonDown("Fire2"))
        {
            Swing();
        }
        Swinging();
        
    }

    /// <summary>
    /// Displays a 3, 2, 1 countdown to player, then sets starting to false
    /// </summary>
    /// <returns></returns>
    IEnumerator ReadyGoCoroutine()
    {
        yield return new WaitForSeconds(.5f);
        threeText.SetActive(true);
        //Beep
        yield return new WaitForSeconds(1);
        threeText.SetActive(false);
        twoText.SetActive(true);
        //Beep
        yield return new WaitForSeconds(1);
        twoText.SetActive(false);
        oneText.SetActive(true);
        //Beep
        yield return new WaitForSeconds(1);
        oneText.SetActive(false);
        //Beep
        starting = false;
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

        // boostHeld is true while the button is pressed
        boostHeld = Input.GetButton("Fire3");

        // boostDown is only true the frame that jump is initially pressed, but this extends it to be true until fixedUpdate() is run once
        if (!boostDown)
        {
            boostDown = Input.GetButtonDown("Fire3");
        }
        
    }

    private bool endReached;
    public int currentLevel;
    public GameObject winScreen;
    public TMP_Text yourTime;
    public TMP_Text bestTime;
    public GameObject nextLevel;
    public GameObject defeatAllEnemies;
    public int numberOfEnemies;

    /// <summary>
    /// Handles and displays end of level screen
    /// </summary>
    private void EndReached()
    {
        if (killedEnemies < numberOfEnemies)
        {
            isDying = true;
            gameOverCanvas.SetActive(true);
            defeatAllEnemies.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        endReached = true;
        //display level stat screen
        //display accuracy in future
        winScreen.SetActive(true);
        switch (currentLevel)
        {
            case 1:
                if (levelTime < PlayerPrefs.GetFloat("Level1BestTime", float.MaxValue))
                {
                    // TODO: Display new best!
                    PlayerPrefs.SetFloat("Level1BestTime", levelTime);
                }
                yourTime.text = FormatTime(levelTime);
                bestTime.text = FormatTime(PlayerPrefs.GetFloat("Level1BestTime"));
                break;
            case 2:
                if (levelTime < PlayerPrefs.GetFloat("Level2BestTime", float.MaxValue))
                {
                    //Display new best!
                    PlayerPrefs.SetFloat("Level2BestTime", levelTime);
                }
                yourTime.text = FormatTime(levelTime);
                bestTime.text = FormatTime(PlayerPrefs.GetFloat("Level2BestTime"));
                break;
            case 3:
                if (levelTime < PlayerPrefs.GetFloat("Level3BestTime", float.MaxValue))
                {
                    //Display new best!
                    PlayerPrefs.SetFloat("Level3BestTime", levelTime);
                }
                yourTime.text = FormatTime(levelTime);
                bestTime.text = FormatTime(PlayerPrefs.GetFloat("Level3BestTime"));
                break;
            default:
                break;
        }
        if (currentLevel == 3)
        {
            nextLevel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Formats given float time to string in MM:SS:mmm format
    /// </summary>
    /// <param name="time">Time to be converted to a string</param>
    /// <returns></returns>
    public static string FormatTime(float time)
    {
        int minutes = (int)time / 60;
        int seconds = (int)time - 60 * minutes;
        int milliseconds = (int)(time * 1000) - minutes * 60000 - 1000 * seconds;
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    /// <summary>
    /// If the player is able to shoot performs bullet SphereCast and sends message to enemy if hit
    /// </summary>
    private void Shoot()
    {
        if (shootCooldown <= 0 && (!swinging || timeSwinging > .4f))
        {
            akShot.Play();
            shootCooldown = .1f;
            if (Physics.SphereCast(mainCamera.position, bulletSize, mainCamera.forward, out RaycastHit hit))
            {
                if (hit.transform.tag == "Enemy")
                {
                    hit.transform.SendMessageUpwards("Killed", true);
                }
            }
        }
    }

    /// <summary>
    /// Begins sword swing animation, sets swinging ang timeSwinging
    /// </summary>
    private void Swing()
    {
        if (!swinging)
        {
            swordAnimator.SetTrigger("Swing");
            gunAnimator.SetTrigger("Swing");
            swinging = true;
            timeSwinging = 0;
        }
    }

    /// <summary>
    /// Performs sword hit detection while swinging
    /// </summary>
    private void Swinging()
    {
        if (swinging)
        {
            timeSwinging += Time.deltaTime;
            if (timeSwinging > .1f && timeSwinging < .3f)
            {
                Collider[] hits = Physics.OverlapCapsule(mainCamera.position + mainCamera.forward, mainCamera.position + mainCamera.forward * 2, 1, enemy);
                foreach (Collider hit in hits)
                {
                    hit.transform.SendMessageUpwards("Killed", false);
                    RefillBoost();
                }
            }
            else if (timeSwinging > .5f)
            {
                swinging = false;
            }
        }
    }

    /// <summary>
    /// Increments killedEnemies
    /// </summary>
    private void IncrementKills()
    {
        killedEnemies++;
    }

    /// <summary>
    /// DEBUG: Displays sword hitbox
    /// </summary>
    private void OnDrawGizmos()
    {
        if (timeSwinging > .1f && timeSwinging < .3f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(mainCamera.position + mainCamera.forward, 1);
            Gizmos.DrawSphere(mainCamera.position + mainCamera.forward * 2, 1);
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

        // TODO: Change to a double click system
        if (boostDown)
        {
            if (boostMeterVal > 0 && !isWallRunning && rechargeBoost && !isBoosting)
            {
                StartBoost();
            }
            // Else play boost fail noise
        } else if (isBoosting && (!boostHeld || boostMeterVal <= 0))
        {
            StopBoost();
        }
        boostDown = false;

        if (rechargeBoost && boostMeterVal < boostMeterLimit)
        {
            boostMeterVal += rechargeRate;
            boostMeterVal = Math.Min(boostMeterVal, boostMeterLimit);
        }

        // Limits player influence on movement while not grounded
        movementCoefficent = 1f;
        if (!grounded)
        {
            movementCoefficent = airborneMovementCoefficent;
        }

        horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Should be able to set this even earlier if needed

        // Movement force is handled depending on which state the player is currently in (switch to case later)
        switch (speedState)
        {
            case 0:
                AccelerateTo(movementDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
                break;
            case 1:
                Wallrun();
                break;
            case 2:
                Boost();
                break;
            case 3:
                AccelerateTo(movementDirection * maxSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
                break;
            case 4:
                AccelerateTo(movementDirection * maxWallSpeed, moveAccel * movementCoefficent, moveAccel * movementCoefficent);
                break;
            default:
                horizontal = horizontal.normalized * 40;
                rb.velocity = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
                Debug.Log("Something went wrong with speedState");
                break;
        }

        //speedDebug.text = string.Format("Velocity: {0:0.0}", horizontal.magnitude);
        if (rechargeBoost || isBoosting)
        {
            boostFill.color = boostReady;
            boostIcon.color = boostReady;
        } else
        {
            boostFill.color = boostNotReady;
            boostIcon.color = boostNotReady;
        }
        boostFill.fillAmount = Mathf.Lerp(.138f, 1, (float)boostMeterVal / boostMeterLimit);
        //BoostText.text = "Boost " + boostMeterVal.ToString();

        //Debug.Log("SpeedState: " + speedState + "\nGrounded: " + grounded + "\nWallrunning: " + isWallRunning + "\nBoosting: " + isBoosting);

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
    /// Sets speedState, isBoosting, and rechargeBoost accordingly
    /// </summary>
    private void StartBoost()
    {
        isBoosting = true;
        speedState = 2;
        //AccelerateTo(movementDirection * maxBoostSpeed, boostAccel, boostAccel); //Can be used as initial boost of extra acceleration
        rechargeBoost = false;
    }

    /// <summary>
    /// Handles movement force while boosting, decrements boostMeterVal
    /// </summary>
    private void Boost()
    {
        AccelerateTo(movementDirection * maxBoostSpeed, boostAccel, boostAccel);
        boostMeterVal--;
    }

    /// <summary>
    /// Sets speedState, isBoosting, and rechargeBoost accordingly
    /// </summary>
    private void StopBoost()
    {
        isBoosting = false;
        if (grounded)
        {
            speedState = 0;
        } else
        {
            speedState = 4;
        }
        Invoke(nameof(RechargeBoost), boostCooldown);
    }

    /// <summary>
    /// Sets rechargeBoost to true
    /// </summary>
    private void RechargeBoost()
    {
        rechargeBoost = true;
    }

    /// <summary>
    /// Instantly refills the boost meter to boostMeterLimit
    /// </summary>
    public void RefillBoost()
    {
        boostMeterVal = boostMeterLimit;
        if (!isBoosting)
        {
            rechargeBoost = true;
        }
        
    }

    private void SideStep()
    {
        //Only while grounded

        //if canSideStep (sideStep cooldown)
        //Quick shift to left or right
        //Translate to nearest lane

        //Notes: Forces probably not good solution
        //Altering position can be bad if they try to sidestep into wall

        //Raycast check for closest lane
        //If hit another object translate to next to that object
        //if lane too close (just to the side of lane plane) check next closest lane (no idea how to do this)
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
        //if (rb.velocity.y < 0)
        //{
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        //}

        // Adds force in direction of player movement and upwards
        // (serves to allow player to use double jump to significantly alter horizontal direction midair)
        Vector3 jumpDirection = new Vector3(movementDirection.x, 0, movementDirection.z);

        // Probably a better way to do this
        switch (speedState)
        {
            case 2:
                AccelerateTo(jumpDirection * maxBoostSpeed, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
                break;
            case 3:
                AccelerateTo(jumpDirection * Mathf.Max(maxSpeed, horizontal.magnitude), horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
                break;
            case 4:
                AccelerateTo(jumpDirection * Mathf.Max(maxWallSpeed, horizontal.magnitude), horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
                break;
            default:
                Debug.Log("Something went wrong with speedState in DoubleJump" + speedState);
                AccelerateTo(jumpDirection * maxSpeed, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce, horizontal.magnitude / Time.fixedDeltaTime + horizontalDoubleJumpForce);
                break;
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
            if (speedState != 2)
            {
                speedState = 0;
            }
            
        }
        else
        {
            grounded = false;

            // state management
            if (speedState == 2 && !isBoosting)
            {
                speedState = 4;
            }
            else if (speedState < 2 && !isWallRunning)
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
        
        //if (isBoosting)
        //{
        //    mouseX = Mathf.Clamp(mouseX, -100 * Time.deltaTime, 100 * Time.deltaTime);
        //}

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
            rb.AddForce(-Vector3.up * 5);
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

                    if (isBoosting)
                    {
                        StopBoost();
                    }

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

        // TODO: Redo force to use accelerateTo

        Vector3 jumpDirection = new Vector3((movementDirection.x/2 + wallHitInfo.normal.x), 0, (movementDirection.z/2 + wallHitInfo.normal.z)); // Can be seperated from vertical component to better control horizontal push off wall
        AccelerateTo(jumpDirection.normalized * maxWallSpeed, wallJumpForce, wallJumpForce);
        rb.AddForce(Vector3.up * jumpForce);
        StopWallRun();
    }

    /// <summary>
    /// Sets cannotRunWallObject to null
    /// </summary>
    private void resetCannotRunOnWallObject()
    {
        cannotRunOnWallObject = null;
    }

    /// <summary>
    /// Decrements health, sets regenCooldown, sets health bar and handles game over screen when health < 1
    /// </summary>
    private void Hit()
    {
        if (!isDying && !isBoosting)
        {
            health--;
            regenCooldown = 3f;
            //TODO: play damage sound or make screen turn red briefly
            healthMeter.fillAmount = 0.25f + health * 0.15f;
            if (health < 1)
            {
                isDying = true;
                gameOverCanvas.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

}
