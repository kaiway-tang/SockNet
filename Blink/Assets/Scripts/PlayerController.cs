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
    bool dead, invuln;

    [SerializeField] PlayerUIHandler uiHandler;
    public static ushort playerObjID;

    public static PlayerController self;
    public Transform trfm;

    private void Awake()
    {
        if (isLocal)
        {            
            self = GetComponent<PlayerController>();
        }
        trfm = transform;
    }

    new void Start()
    {
        if (isLocal)
        {
            buffer16 = new byte[16];
            buffer6 = new byte[6];
            buffer4 = new byte[4];

            NetworkManager.InitializeNetObj(self, 0);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        hpScript.OnDamage += GetComponent<PlayerController>().OnDamage;
        hpScript.useDefaultBehavior = false;
        PlayerObj.parent = null;

        uiHandler.SetHP(100);
        mana = 100;
        uiHandler.mana = 100;
    }

    private void Update()
    {
        if (dead)
        {
            trfm.position = Vector3.zero + Vector3.up * 2;
            hpScript.HP = 100;
            mana = 100;

            if (isLocal)
            {
                uiHandler.mana = 100;
                uiHandler.SetHP(100);
            }            
            
            hpScript.SyncHP();
            dead = false;
            return;
        }

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

                    buffer6[5] = 0;
                }
                else if (mana > 20)
                {
                    velocityVect = rb.velocity;
                    velocityVect.y = jumpPower * dJumpMultiplier;
                    rb.velocity = velocityVect;
                    mana -= 20;

                    buffer6[5] = 1;
                }

                buffer6[2] = 4;
                NetworkManager.SetBufferTime(buffer6, 3);
                NetworkManager.Send(buffer6);
            }

            if (updateTimeout < 0)
            {
                SendTrfm();
            }
            updateTimeout -= Time.deltaTime;

            HandleRotationInput();
            HandleAbilities();
        }

        HandleTimers();
        UpdateVelocity();
    }

    void HandleTimers()
    {
        if (slashTimer > 0)
        {
            slashTimer -= Time.deltaTime;
            if (slashTimer <= 0)
            {
                slashFX.Stop();
            }
        }

        if (vanished)
        {
            mana -= Time.deltaTime * 10;
            if (mana < 0)
            {
                mana = 0;
                EndVanish(true);
            }
            if (isLocal) { uiHandler.mana = mana; }
        }
    }

    #region NETWORKING

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);

        if (isLocal) { playerObjID = ID; }        
        hpScript.objID = ID;
        //Debug.Log("player received ID: " + ID);

        if (isLocal)
        {
            NetworkManager.SetBufferUShort(buffer16, playerObjID);
            NetworkManager.SetBufferUShort(buffer6, playerObjID);
            NetworkManager.SetBufferUShort(buffer4, playerObjID);
        }        
    }

    float updateTimeout;
    bool keyFwd = false, keyBack = false, keyLeft = false, keyRight = false;

    int temp;
    byte[] buffer16;
    byte[] buffer6;
    byte[] buffer4;

    public void SendAction()
    {
        if (!IsConnected()) { return; }

        buffer16[2] = 1;
        buffer16[3] = projectileType;
        NetworkManager.SetBufferTime(buffer16, 4);
        NetworkManager.SetBufferCoords(buffer16, PlayerObj.position, 6);

        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(PlayerObj.eulerAngles.y), 12);
        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 14);

        NetworkManager.Send(buffer16);
    }
    public void SendTrfm()
    {
        updateTimeout = 0.2f;
        if (!IsConnected()) { return; }
        //NetworkManager.SetBufferUShort(buffer16, playerObjID);
        buffer16[2] = 0;
        NetworkManager.SetBufferTime(buffer16, 3);

        NetworkManager.SetBufferCoords(buffer16, trfm.position);

        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(PlayerObj.eulerAngles.y), 11);
        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 13);

        temp = keyFwd ? 8 : 0;
        temp += keyBack ? 4 : 0;
        temp += keyLeft ? 2 : 0;
        temp += keyRight ? 1 : 0;

        buffer16[15] = (byte)temp;
    
        NetworkManager.Send(buffer16);
    }

    public override void NetworkUpdate(byte[] buffer)
    {        
        if (buffer[2] == 223)
        {
            Debug.Log("Player recv: " + buffer[0] + " " + buffer[1] + " " + buffer[2] + " " + buffer[3] + " " + buffer[4]);
            hpScript.ResolveHP(buffer);
        }

        if (isLocal) return;
        if (buffer[2] == 0)
        {
            HandleTrfmUpdate(buffer);
        }
        else if (buffer[2] == 1)
        {
            mana -= 20;

            PlayerObj.position = NetworkManager.GetBufferCoords(buffer, 6);
            SetRotation(NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 14)), NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 12)));

            Instantiate(projectiles[buffer[3]], firePoint.position, firePoint.rotation)
                .GetComponent<Projectile>().Init(objID, NetworkManager.GetBufferDelta(buffer, 4), isLocal);
                //.GetComponent<Projectile>().Init(objID, 0);
        }
        else if (buffer[2] == 2)
        {
            mana -= 20;
            if (buffer[3] == 1) { CastVanish(); }
            else { EndVanish(false); }
        }
        else if (buffer[2] == 3)
        {
            CastSlash();
        }
        else if (buffer[2] == 4)
        {
            velocityVect = rb.velocity;
            velocityVect.y = jumpPower;

            if (buffer[5] == 1)
            {
                velocityVect.y *= dJumpMultiplier;
                mana -= 20;
            }
            rb.velocity = velocityVect;
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
    [SerializeField] MeshRenderer[] playerMeshes;

    [SerializeField] Transform firePoint;
    [SerializeField] ParticleSystem slashFX;
    [SerializeField] Clone activeClone;
    float mana;

    float slashTimer;

    bool vanished;

    RaycastHit rayHit;
    float rayHitDist;

    void HandleAbilities()
    {
        if (mana > 20)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(camTrfm.position, camTrfm.forward, out rayHit, dashDistance, Tools.terrainMask))
                {
                    rayHitDist = rayHit.distance;
                    if (Physics.Raycast(camTrfm.position, camTrfm.forward, out rayHit, rayHitDist, Tools.hurtboxMask))
                    {
                        rayHit.collider.GetComponent<HPEntity>().TakeDamage(80, objID, true);
                    }                   
                    transform.position = PlayerObj.position + camTrfm.forward * rayHitDist + Vector3.up * 1.1f;
                }
                else
                {
                    if (Physics.Raycast(camTrfm.position, camTrfm.forward, out rayHit, dashDistance, Tools.hurtboxMask))
                    {
                        rayHit.collider.GetComponent<HPEntity>().TakeDamage(80, objID, true);
                    }
                    transform.position = PlayerObj.position + camTrfm.forward * dashDistance;
                }

                mana -= 20;
                CastSlash();
                SendTrfm();
            }
            if (Input.GetMouseButtonDown(1))
            {
                Instantiate(projectiles[projectileType], firePoint.position, firePoint.rotation).GetComponent<Projectile>().Init(objID, 0, isLocal);
                SendAction();
                mana -= 20;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                CastVanish();                
                mana -= 20;
                buffer4[2] = 2;
                buffer4[3] = 1;
                NetworkManager.Send(buffer4);
            }            
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if (vanished) { EndVanish(true); }
        }


        if (!vanished && mana < 100)
        {
            mana += Time.deltaTime * 10;
            if (mana > 100) { mana = 100; }

            if (isLocal) { uiHandler.mana = mana; }            
        }
    }

    void CastSlash()
    {        
        //PlayerObj.position = transform.position;
        slashFX.Play();
        slashTimer = 0.2f;
    }

    void CastVanish()
    {
        activeClone = Instantiate(clone, trfm.position, trfm.rotation).GetComponent<Clone>();
        activeClone.Init(PlayerObj.position);

        for (int i = 0; i < playerMeshes.Length; i++)
        {
            playerMeshes[i].enabled = false;
        }
        vanished = true;
    }

    void EndVanish(bool sendUpdate)
    {
        if (sendUpdate)
        {
            buffer4[2] = 2;
            buffer4[3] = 0;
            NetworkManager.Send(buffer4);
        }

        for (int i = 0; i < playerMeshes.Length; i++)
        {
            playerMeshes[i].enabled = true;
        }

        activeClone.End();
        vanished = false;
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

    public void OnDamage(ushort amount, ushort sourceID)
    {
        //flag
        hpScript.damageFX.Play();
        hpScript.HP -= amount;

        if (hpScript.HP < 0.01)
        {
            hpScript.HP = 0;
            dead = true;            
        }

        if (isLocal) { uiHandler.SetHP(hpScript.HP); }
    }

    [SerializeField] float targetYRot, targetXRot;
    Vector3 eulerAngles;
    [SerializeReference] TextMeshProUGUI tmp;
    void FixedUpdate()
    {
        if (slashTimer > 0)
        {
            rb.velocity = Vector3.zero;
        }

        PlayerObj.position += (transform.position - PlayerObj.position) * lerpRate;     

        if (!isLocal)
        {
            SetRotation(Tools.RotationalLerp(camTrfm.localEulerAngles.x, targetXRot, 0.5f), Tools.RotationalLerp(PlayerObj.localEulerAngles.y, targetYRot, 0.5f));
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
