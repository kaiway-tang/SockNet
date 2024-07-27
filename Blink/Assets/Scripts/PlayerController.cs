using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : NetworkObject
{
    [SerializeField] float speed;
    [SerializeField] float jumpPower, dJumpMultiplier;

    public Transform PlayerObj;
    [SerializeField] float lerpRate;

    [SerializeField] Transform camTrfm;
    [SerializeField] float sensitivity;

    [SerializeField] Rigidbody rb;
    [SerializeField] bool isLocal;

    [SerializeField] GroundDetect groundDetect;
    [SerializeField] HPEntity hpScript;
    public static ushort playerObjID;

    public static PlayerController self;

    private void Awake()
    {
        self = GetComponent<PlayerController>();
    }

    new void Start()
    {
        if (isLocal)
        {
            NetworkManager.InitializeNetObj(self, 0);
        }        

        PlayerObj.parent = null;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);
        playerObjID = ID;
        hpScript.objID = ID;

        Debug.Log("player received ID: " + ID);
    }

    float updateTimeout;
    bool keyFwd = false, keyBack = false, keyLeft = false, keyRight = false;
    private void Update()
    {
        if (isLocal)
        {
            if (UpdateKeyStatuses())
            {
                SendNetworkUpdate();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (OnGround())
                {
                    velocityVect = rb.velocity;
                    velocityVect.y = jumpPower;
                    rb.velocity = velocityVect;
                }
                else if (mana > 20)
                {
                    velocityVect = rb.velocity;
                    velocityVect.y = jumpPower * dJumpMultiplier;
                    rb.velocity = velocityVect;
                    mana -= 20;
                }
            }

            if (updateTimeout < 0)
            {
                SendNetworkUpdate();
            }
            updateTimeout -= Time.deltaTime;

            HandleRotationInput();
            HandleAbilities();
        }

        UpdateVelocity();        
    }

    void SendNetworkUpdate()
    {
        if (IsConnected())
        {
            NetworkManager.UpdatePlayerInput(transform.position, keyFwd, keyBack, keyLeft, keyRight);
            updateTimeout = 0.2f;
        }        
    }

    public override void NetworkUpdate(byte[] buffer)
    {
        if (isLocal) return;

        transform.position = NetworkManager.GetBufferCoords(buffer);
        targetYRot = NetworkManager.DecodeValue(NetworkManager.GetBufferUShort(buffer, 10));        

        keyFwd = ((buffer[12] >> 3) & 0x01) == 1;
        keyBack = ((buffer[12] >> 2) & 0x01) == 1;
        keyLeft = ((buffer[12] >> 1) & 0x01) == 1;
        keyRight = (buffer[12] & 0x01) == 1;

        Update();

        //transform.position += rb.velocity * NetworkManager.GetBufferDelay(buffer, 2);
    }

    [SerializeField] float dashDistance;
    [SerializeField] GameObject shuriken, clone;
    [SerializeField] Transform firePoint;
    float mana;
    void HandleAbilities()
    {
        if (mana > 20)
        {
            if (Input.GetMouseButtonDown(0))
            {
                transform.position = PlayerObj.position + camTrfm.forward * dashDistance;
                //PlayerObj.position = transform.position;
                mana -= 20;
            }
            if (Input.GetMouseButtonDown(1))
            {
                Instantiate(shuriken, firePoint.position, firePoint.rotation).GetComponent<Projectile>().ownerID = objID;
                mana -= 20;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Instantiate(clone, PlayerObj.position, PlayerObj.rotation).GetComponent<Clone>().lerpDest = transform.position;
                mana -= 20;
            }
        }
        

        if (mana < 100)
        {
            mana += Time.deltaTime * 10;
            if (mana > 100) { mana = 100; }
        }
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

    [SerializeField] float yMouse, xMouse;
    void HandleRotationInput()
    {
        yMouse = -Input.GetAxis("MY") * sensitivity;      //right now camera is flipped, and rotates indefintley, so fix that and set to max 90 degrees
        xMouse = Input.GetAxis("MX") * sensitivity;

        camTrfm.Rotate(Vector3.right * yMouse);
        PlayerObj.Rotate(Vector3.up * xMouse);     
        
        if (Mathf.Abs(xMouse) > 0.4f && updateTimeout < 0.15f)
        {
            SendNetworkUpdate();
        }
    }

    bool OnGround()
    {
        return groundDetect.touchCount > 0;
    }

    float targetYRot;
    Vector3 eulerAngles;
    [SerializeReference] TextMeshProUGUI tmp;
    void FixedUpdate()
    {
        //tmp.text = "Mana: " + Mathf.RoundToInt(mana).ToString();
        PlayerObj.position += (transform.position - PlayerObj.position) * lerpRate;     

        if (!isLocal)
        {
            eulerAngles = PlayerObj.eulerAngles;
            eulerAngles.y = targetYRot;
            //PlayerObj.eulerAngles += (eulerAngles - PlayerObj.eulerAngles) * 0.2f;
            PlayerObj.eulerAngles = eulerAngles;
        }
    }
}
