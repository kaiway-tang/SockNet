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
    public static Transform trfm;

    private void Awake()
    {
        if (isLocal)
        {
            trfm = transform;
            self = GetComponent<PlayerController>();
        }        
    }

    new void Start()
    {
        if (isLocal)
        {
            trfmInputBuffer = new byte[16];           
            actionInputBuffer = new byte[16];            

            NetworkManager.InitializeNetObj(self, 0);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        hpScript.OnDamage += GetComponent<PlayerController>().OnDamage;
        hpScript.useDefaultBehavior = false;
        PlayerObj.parent = null;        
    }

    #region NETWORKING

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);
        if (isLocal)
        {
            playerObjID = ID;
            hpScript.objID = ID;
            Debug.Log("player received ID: " + ID);

            NetworkManager.SetBufferUShort(trfmInputBuffer, playerObjID);
            NetworkManager.SetBufferUShort(actionInputBuffer, playerObjID);
        }      
    }

    float updateTimeout;
    bool keyFwd = false, keyBack = false, keyLeft = false, keyRight = false;
    private void Update()
    {
        if (isLocal)
        {
            if (UpdateKeyStatuses())
            {
                SendTrfm();
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
                SendTrfm();
            }
            updateTimeout -= Time.deltaTime;

            HandleRotationInput();
            HandleAbilities();
        }

        UpdateVelocity();        
    }

    int temp;
    byte[] trfmInputBuffer;
    byte[] actionInputBuffer;

    public void SendAction()
    {
        if (!IsConnected()) { return; }

        actionInputBuffer[2] = 1;
        actionInputBuffer[3] = projectileType;
        NetworkManager.SetBufferTime(actionInputBuffer, 4);
        NetworkManager.SetBufferCoords(actionInputBuffer, PlayerObj.position, 6);

        NetworkManager.SetBufferUShort(actionInputBuffer, NetworkManager.EncodePosValue(PlayerObj.eulerAngles.y), 12);
        NetworkManager.SetBufferUShort(actionInputBuffer, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 14);

        NetworkManager.Send(actionInputBuffer);
    }
    public void SendTrfm()
    {
        updateTimeout = 0.2f;
        if (!IsConnected()) { return; }
        //NetworkManager.SetBufferUShort(trfmInputBuffer, playerObjID);
        trfmInputBuffer[2] = 0;
        NetworkManager.SetBufferTime(trfmInputBuffer, 3);

        NetworkManager.SetBufferCoords(trfmInputBuffer, trfm.position);

        NetworkManager.SetBufferUShort(trfmInputBuffer, NetworkManager.EncodePosValue(PlayerObj.eulerAngles.y), 11);
        NetworkManager.SetBufferUShort(trfmInputBuffer, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 13);

        temp = keyFwd ? 8 : 0;
        temp += keyBack ? 4 : 0;
        temp += keyLeft ? 2 : 0;
        temp += keyRight ? 1 : 0;

        trfmInputBuffer[15] = (byte)temp;
    
        NetworkManager.Send(trfmInputBuffer);
    }

    public override void NetworkUpdate(byte[] buffer)
    {
        if (isLocal) return;
        if (buffer[2] == 0)
        {
            HandleTrfmUpdate(buffer);
        }
        else if (buffer[2] == 1)
        {
            PlayerObj.position = NetworkManager.GetBufferCoords(buffer, 6);
            SetRotation(NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 14)), NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 12)));

            Debug.Log("buffer delta: " + NetworkManager.GetBufferDelta(buffer, 4));
            Instantiate(projectiles[buffer[3]], firePoint.position, firePoint.rotation)
                .GetComponent<Projectile>().Init(objID, NetworkManager.GetBufferDelta(buffer, 4));
                //.GetComponent<Projectile>().Init(objID, 0);
        }
        else if (buffer[3] == 2)
        {

        }
    }

    void HandleTrfmUpdate(byte[] buffer)
    {
        transform.position = NetworkManager.GetBufferCoords(buffer);
        targetYRot = NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 11));
        targetXRot = NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 13));

        keyFwd = ((buffer[15] >> 3) & 0x01) == 1;
        keyBack = ((buffer[15] >> 2) & 0x01) == 1;
        keyLeft = ((buffer[15] >> 1) & 0x01) == 1;
        keyRight = (buffer[15] & 0x01) == 1;

        Update();

        //transform.position += rb.velocity * NetworkManager.GetBufferDelay(buffer, 2);
    }

    #endregion

    #region HANDLE_INPUT

    [SerializeField] float dashDistance;

    byte projectileType;
    [SerializeField] GameObject[] projectiles;
    [SerializeField] GameObject clone;
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
                Instantiate(projectiles[projectileType], firePoint.position, firePoint.rotation).GetComponent<Projectile>().Init(objID, 0);
                SendAction();
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

    void HandleRotationInput()
    {
        yMouse = -Input.GetAxis("MY") * sensitivity;      //right now camera is flipped, and rotates indefintley, so fix that and set to max 90 degrees
        xMouse = Input.GetAxis("MX") * sensitivity;

        camTrfm.Rotate(Vector3.right * yMouse);
        PlayerObj.Rotate(Vector3.up * xMouse);

        if (Mathf.Abs(xMouse) > 0.1f && updateTimeout < 0.05f + Mathf.Abs(xMouse) * 0.1f && updateTimeout < 0.15f)
        {
            SendTrfm();
        }
    }

    #endregion

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

    bool OnGround()
    {
        return groundDetect.touchCount > 0;
    }

    public void OnDamage(int amount, ushort sourceID)
    {
        hpScript.damageFX.Play();
        hpScript.HP -= amount;
    }

    [SerializeField] float targetYRot, targetXRot;
    Vector3 eulerAngles;
    [SerializeReference] TextMeshProUGUI tmp;
    void FixedUpdate()
    {
        PlayerObj.position += (transform.position - PlayerObj.position) * lerpRate;     

        if (!isLocal)
        {
            SetRotation(Tools.RotationalLerp(camTrfm.localEulerAngles.x, targetXRot, 0.5f), Tools.RotationalLerp(PlayerObj.localEulerAngles.y, targetYRot, 0.5f));
        }
        else
        {
            tmp.text = "Mana: " + Mathf.RoundToInt(mana).ToString();
        }
    }

    void SetRotation(float x, float y)
    {
        eulerAngles = PlayerObj.eulerAngles;
        eulerAngles.y = y;
        PlayerObj.eulerAngles = eulerAngles;

        eulerAngles = camTrfm.localEulerAngles;
        eulerAngles.x = x;
        camTrfm.localEulerAngles = eulerAngles;
    }
}
