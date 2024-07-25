using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : NetworkObject
{
    [SerializeField] float speed;
    [SerializeField] float jumpPower;

    public Transform PlayerObj;
    [SerializeField] float lerpRate;

    [SerializeField] Transform camTrfm;
    [SerializeField] float sensitivity;

    [SerializeField] Rigidbody rb;
    [SerializeField] bool isLocal;

    public static ushort playerObjID;

    public static PlayerController self;

    private void Awake()
    {
        PlayerController.self = GetComponent<PlayerController>();
    }

    new void Start()
    {
        objID = NetworkManager.GetNewObjID(GetComponent<NetworkObject>(), true);
        playerObjID = objID;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    bool keyFwd = false, keyBack = false, keyLeft = false, keyRight = false;
    private void Update()
    {
        if (isLocal)
        {
            if (UpdateKeyStatuses())
            {
                NetworkManager.UpdatePlayerInput(transform.position, keyFwd, keyBack, keyLeft, keyRight);
            }
            if (Input.GetKeyDown(KeyCode.Space)) { rb.velocity += Vector3.up * jumpPower; }
        }               

        UpdateVelocity();

        HandleRotation();
    }

    bool inputUpdate;
    bool UpdateKeyStatuses()
    {
        inputUpdate = false;

        if (Input.GetKeyDown(KeyCode.W) && keyFwd == false) { keyFwd = true; inputUpdate = true; }
        else if (Input.GetKeyUp(KeyCode.W) && keyFwd == true) { keyFwd = false; inputUpdate = true; }

        if (Input.GetKeyDown(KeyCode.S) && keyBack == false) { keyBack = true; inputUpdate = true; }
        else if (Input.GetKeyUp(KeyCode.S) && keyBack == true) { keyBack = false; inputUpdate = true; }

        if (Input.GetKeyDown(KeyCode.A) && keyLeft == false) { keyLeft = true; inputUpdate = true; }
        else if (Input.GetKeyUp(KeyCode.A) && keyLeft == true) { keyLeft = false; inputUpdate = true; }

        if (Input.GetKeyDown(KeyCode.D) && keyRight == false) { keyRight = true; inputUpdate = true; }
        else if (Input.GetKeyUp(KeyCode.D) && keyRight == true) { keyRight = false; inputUpdate = true; }

        return inputUpdate;
    }

    Vector3 velocityVect;
    void UpdateVelocity()
    {
        velocityVect.x = 0;
        velocityVect.z = 0;        

        if (keyFwd)
        {
            if (!keyBack)
            {
                if (keyLeft)
                {
                    if (!keyRight)
                    {
                        velocityVect.x = -0.707f * speed;
                        velocityVect.z = 0.707f * speed * 1.2f;
                    }
                }
                else if (keyRight)
                {
                    velocityVect.x = 0.707f * speed;
                    velocityVect.z = 0.707f * speed * 1.2f;
                }
                else
                {
                    velocityVect.z = speed * 1.2f;
                }
            }
        }
        else if (keyBack)
        {
            if (keyLeft)
            {
                if (!keyRight)
                {
                    velocityVect.x = -0.707f * speed;
                    velocityVect.z = -0.707f * speed;
                }
            }
            else if (keyRight)
            {
                velocityVect.x = 0.707f * speed;
                velocityVect.z = -0.707f * speed;
            }
            else
            {
                velocityVect.z = -speed;
            }
        }
        else
        {
            if (keyLeft)
            {
                velocityVect.x = -speed;
            }
            else if (keyRight)
            {
                velocityVect.x = speed;
            }
        }

        velocityVect = PlayerObj.forward * velocityVect.z + PlayerObj.right * velocityVect.x;
        velocityVect.y = rb.velocity.y;
        rb.velocity = velocityVect;
    }

    float yMouse, xMouse;
    void HandleRotation()
    {
        yMouse = -Input.GetAxis("MY") * sensitivity;      //right now camera is flipped, and rotates indefintley, so fix that and set to max 90 degrees
        xMouse = Input.GetAxis("MX") * sensitivity;

        camTrfm.Rotate(Vector3.right * yMouse);
        PlayerObj.Rotate(Vector3.up * xMouse);
    }

    void HandleMovement()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PlayerObj.position += (transform.position - PlayerObj.position) * lerpRate;
    }
}
