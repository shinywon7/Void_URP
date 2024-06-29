using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplePlayer : MonoBehaviour
{
    public float mouseSensitivity = 10;
    public Vector2 pitchMinMax = new Vector2(-90, 90);
    public Vector2 holdPitchMinMax = new Vector2(-30, 80);
    public float rotationSmoothTime = 0.1f;

    float yaw;
    float pitch;
    float holdPitch;
    float smoothYaw;
    float smoothPitch;
    float yawSmoothV;
    float pitchSmoothV;

    public float moveSpeed;
    public float walkSpeed = 4f;
    public float sprintSpeed = 6f;
    public float groundDrag = 6f;
    public float airDrag = 2f;
    public float gravity = 9.8f;
    float movementMultiplier = 10f;
    float airMultiplier = 0.4f;
    float acceleration = 10f;

    public bool isGrounded;


    Vector3 moveDir;
    public float jumpForce;
    public Transform groundCheck;

    public LayerMask groundMask;

    public Rigidbody rigidBody;
    public static Transform headTransform;
    public static Transform playerTransform;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        headTransform = Camera.main.transform;
    }


    public void MouseLook()
    {
        float mX = Input.GetAxisRaw("Mouse X");
        float mY = Input.GetAxisRaw("Mouse Y");

        // Verrrrrry gross hack to stop camera swinging down at start
        float mMag = Mathf.Sqrt(mX * mX + mY * mY);
        if (mMag > 5)
        {
            mX = 0;
            mY = 0;
        }

        yaw += mX * mouseSensitivity;
        pitch -= mY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, rotationSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, rotationSmoothTime);

        holdPitch = Mathf.Clamp(smoothPitch, holdPitchMinMax.x, holdPitchMinMax.y);

        transform.eulerAngles = Vector3.up * smoothYaw;
        headTransform.localEulerAngles = Vector3.right * smoothPitch;
        //VoidManager.holdTransform.localEulerAngles = Vector3.right * holdPitch;
    }
    public void MyInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveDir = transform.forward * vertical + transform.right * horizontal;
    }
    void ControlDrag()
    {
        rigidBody.drag = isGrounded ? groundDrag : airDrag;
    }
    void ControlSpeed()
    {
        moveSpeed = Mathf.Lerp(moveSpeed, (Input.GetKey(KeyCode.LeftShift) && isGrounded) ? sprintSpeed : walkSpeed, acceleration * Time.deltaTime);
    }
    void Move()
    {
        if (isGrounded) rigidBody.AddForce(moveDir.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
        else rigidBody.AddForce(moveDir.normalized * moveSpeed * airMultiplier, ForceMode.Acceleration);
    }
    void Jump()
    {
        rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);
        rigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.4f, groundMask);

        MyInput();
        ControlDrag();
        ControlSpeed();
        MouseLook();
    }
    void GetGravity()
    {
        rigidBody.AddForce(Vector3.up * -gravity, ForceMode.Acceleration);
    }
    void FixedUpdate()
    {
        Move();
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) Jump();
        GetGravity();
    }
}
