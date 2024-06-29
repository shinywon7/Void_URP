using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    public float mouseSensitivity = 10;
    public Vector2 pitchMinMax = new Vector2(-90, 90);
    public Vector2 holdPitchMinMax = new Vector2(-30, 80);
    public float rotationSmoothTime = 0.2f;
    float rotationSpeed;

    float yaw;
    float pitch;
    float holdPitch;

    public float speed = 4f;
    public float walkSpeed = 4f;
    public float sprintSpeed = 6f;
    public float jump = 1f;
    public float groundDrag;
    public float airDrag;
    public Vector3 initGravity;
    Vector3 gravity;

    public bool isGrounded;


    Vector3 moveDir;
    public float jumpForce;
    public Transform groundCheck;
    public float groundDistance = 0.4f;

    public LayerMask frontGroundMask, backGroundMask;
    LayerMask groundMask;

    public static Transform headTransform;
    public static Transform playerTransform;
    public static Transform backCamTransform;
    public static Transform yawAxisTransform;
    public static Camera mainCam;

    public static Action flip;
    public static bool isFlipped = false;
    public static Rigidbody rb;
    public Rigidbody fakeRigidbody;
    public GameObject fakePlayer;
    float voidDist;
    Vector3 velocity;
    float side = 1;

    Material mat;
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerTransform = transform;
        headTransform = Camera.main.transform;
        yawAxisTransform = transform.Find("YawAxis");
        backCamTransform = transform.Find("BackCamLoca");
        mainCam = Camera.main;
        mat = GetComponent<MeshRenderer>().material;
        groundMask = frontGroundMask;
        VoidManager.voidFade += VoidFade;
        VoidManager.voidRearrange += VoidRearrange;
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

        //transform.Rotate(Vector3.up, mX * mouseSensitivity, Space.Self);
        //transform.localEulerAngles = Vector3.up * yaw;
        yawAxisTransform.Rotate(Vector3.up, mX * mouseSensitivity, Space.Self);
        headTransform.localEulerAngles = Vector3.right * pitch;
    }
    public void MyInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveDir = transform.forward * vertical + transform.right * horizontal;
    }
    void Movement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, frontGroundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity = 2f * initGravity.normalized;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        //controller.Move(move * speed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity = -Mathf.Sqrt(Vector3.Magnitude(jump * 2 * initGravity))*initGravity.normalized;
        }

        velocity += initGravity * Time.deltaTime;

        rb.velocity = velocity + move * speed;
        //rb.MovePosition(rb.position + ((velocity + move * speed) * Time.deltaTime));
    }
    void Movement1()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        float drag = isGrounded ? groundDrag : airDrag;
        rb.drag = drag;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity = 2f * initGravity.normalized;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (yawAxisTransform.right * x + yawAxisTransform.forward * z).normalized;
        //controller.Move(move * speed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(-gravity.normalized*jump, ForceMode.VelocityChange);   
        }
        
        rb.AddForce(gravity, ForceMode.Acceleration);
        if(isGrounded) rb.AddForce(move*speed, ForceMode.Acceleration);
    }
    bool disabled = false;

    public void ChangeGravityAxis(){
        float angle = Vector3.Angle(-transform.up,gravity);
        Vector3 axis = Vector3.Cross(-transform.up,gravity);
        if(axis == Vector3.zero) axis = transform.forward;
        float speed = Vector3.Magnitude(rb.velocity);
        float newAngle = Mathf.SmoothDamp(angle,0,ref rotationSpeed, rotationSmoothTime*(1+Mathf.Pow(speed/3,2)));
        transform.Rotate(axis.normalized,angle - newAngle,Space.World);
    }

    public void Update()
    {
        voidDist = VoidManager.voidTransform.InverseTransformPoint(headTransform.position).y;

        gravity = voidDist > 0 ? initGravity : VoidManager.gravity;
        //gravity = Vector3.Lerp(initGravity,VoidManager.gravity,Mathf.SmoothStep(0.5f,-0.5f,voidDist));
        ChangeGravityAxis();

        if(isFlipped) isFlipped = false;
        
        if(voidDist < -VoidManager.halfVoidWidth)
        {
            VoidManager.VoidFlip();
            flip.Invoke();
            isFlipped = true;
            (fakePlayer.layer, transform.gameObject.layer) = (transform.gameObject.layer, fakePlayer.layer);
            groundMask = groundMask == frontGroundMask ? backGroundMask : frontGroundMask;
            voidDist = VoidManager.voidTransform.InverseTransformPoint(headTransform.position).y;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            disabled = !disabled;
        }

        if (disabled)
        {
            return;
        }

        MyInput();
        MouseLook();
        Movement1();
        SetMaterial();
    }
    public void VoidRearrange(){
        if(voidDist < 0){
            float voidDistRate = -voidDist/VoidManager.voidWidth;
            transform.position -= VoidManager.voidTransform.up * VoidManager.deltaVoidWidth * voidDistRate;
        }
    }
    public void SetMaterial(){
        Vector4 velocity = rb.velocity;
        mat.SetVector("_Velocity",transform.localToWorldMatrix*velocity);
        mat.SetVector("_AngularVelocity",Vector4.zero);
    }
    public void VoidFade(){
        transform.gameObject.layer = LayerMask.NameToLayer("Player");
        fakePlayer.layer = LayerMask.NameToLayer("PlayerBack");
        groundMask = frontGroundMask;
    }
    public void LateUpdate()
    {
        Sync();
        backCamTransform.SetPositionAndRotation(headTransform.position + VoidManager.voidTransform.up * VoidManager.voidWidth, headTransform.rotation);
    }
    public void Sync()
    {
        fakeRigidbody.position = transform.position + VoidManager.voidTransform.up * VoidManager.voidWidth;
        fakeRigidbody.rotation = transform.rotation;
        fakeRigidbody.velocity = rb.velocity;
    }
    public void FixedUpdate()
    {
        
    }
}
