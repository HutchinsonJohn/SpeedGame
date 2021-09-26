using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPlayerMovement : MonoBehaviour
{

    // Assingables
    private Transform mainCamera;
    public Transform orientation;

    private Rigidbody rb;

    // Variables
    private float leftRightInput, forwardBackwardInput;
    public bool jumpHeld, jumpDown, boostHeld, boostDown;
    private Vector3 tiltInput, projForward, movementDirection, horizontal;

    // Grounded
    public bool grounded;
    public LayerMask whatIsGround;
    RaycastHit groundHitInfo;

    // Rotation and look
    private float xRotation;
    private float sensitivity = 1f;
    private float desiredX;
    private float wallRunCameraTilt = 0;
    public float maxWallRunCameraTilt = 10;
    public float cameraTiltSpeed = 3;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();
        Look();
        CheckGround();
        SetMovementDirection();
        horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }

    private void FixedUpdate()
    {
        Debug.Log(rb.velocity.magnitude);
        rb.AddForce(10000 * Time.deltaTime * movementDirection);

        float maxDrag = Mathf.Max(10, horizontal.magnitude);
        float drag = horizontal.magnitude / maxDrag;
        rb.AddForce(10000 * drag * Time.deltaTime * -horizontal.normalized);
    }

    //private void Drag()
    //{
    //    float minDrag;
    //    switch (speedState)
    //    {
    //        case 0:
    //            minDrag = 10;
    //            break;
    //        case 1:
    //            minDrag = 15;
    //            break;
    //        case 2:
    //            minDrag = 20;
    //            break;
    //        default:
    //            minDrag = 10;
    //            break;
    //    }
    //    float maxDrag = Mathf.Max(minDrag, horizontal.magnitude);
    //    float drag = horizontal.magnitude / maxDrag;
    //    //air coefficent
    //    rb.AddForce(-horizontal.normalized * 10000 * drag * movementCoefficent * Time.deltaTime);
    //}

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
    /// Sets grounded to true if there is a ground layer object within a certain distance below player RigidBody, false otherwise.
    /// Also resets readyToDoubleJump to true if grounded is set to true
    /// </summary>
    private void CheckGround()
    {
        // Checks for ground layer object 
        if (Physics.Raycast(rb.position, -Vector3.up, out groundHitInfo, 1.15f, whatIsGround))
        {
            grounded = true;
            //readyToDoubleJump = true;

            // state management
            //if (speedState != 2)
            //{
            //    speedState = 0;
            //}

        }
        else
        {
            grounded = false;

            // state management
            //if (speedState == 2 && !isBoosting)
            //{
            //    speedState = 4;
            //}
            //else if (speedState < 2 && !isWallRunning)
            //{
            //    speedState = 3;
            //}
        }
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
        Vector3 rot = mainCamera.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Tilts camera while wallrunning
        //if (isWallRunning)
        //{

        //    float dot = Vector3.Dot(Vector3.Cross(orientation.forward, wallHitInfo.normal), new Vector3(1, 1, 1));

        //    if (wallRunCameraTilt < maxWallRunCameraTilt && dot < 0)
        //    {
        //        wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;

        //        // Limits wallRunCameraTilt to exactly limit
        //        if (wallRunCameraTilt > maxWallRunCameraTilt)
        //            wallRunCameraTilt = maxWallRunCameraTilt;

        //    }
        //    else if (wallRunCameraTilt > -maxWallRunCameraTilt && dot > 0)
        //    {
        //        wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;

        //        // Limits wallRunCameraTilt to exactly limit
        //        if (wallRunCameraTilt < -maxWallRunCameraTilt)
        //            wallRunCameraTilt = -maxWallRunCameraTilt;
        //    }
        //}

        //// Tilts camera back again
        //if (wallRunCameraTilt > 0 && !isWallRunning)
        //{
        //    if (wallRunCameraTilt <= .5) // At 60fps, max camera tilt error is .5, at 30fps its 1.0 with maxtilt 10 and tiltspeed 3
        //    {
        //        wallRunCameraTilt = 0;
        //    }
        //    else
        //    {
        //        wallRunCameraTilt -= Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;
        //    }
        //}
        //else if (wallRunCameraTilt < 0 && !isWallRunning)
        //{
        //    if (wallRunCameraTilt >= -.5) // At 60fps, max camera tilt error is .5, at 30fps its 1.0 with maxtilt 10 and tiltspeed 3
        //    {
        //        wallRunCameraTilt = 0;
        //    }
        //    else
        //    {
        //        wallRunCameraTilt += Time.deltaTime * maxWallRunCameraTilt * cameraTiltSpeed;
        //    }
        //}

        //Perform the rotations
        mainCamera.localRotation = Quaternion.Euler(xRotation, desiredX, wallRunCameraTilt);
        orientation.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

}
