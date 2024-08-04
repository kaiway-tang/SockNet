using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using UnityEngine.SceneManagement;

public class PlayerController : NetworkObject
{
    [SerializeField] float speed;
    [SerializeField] float jumpPower, dJumpMultiplier;

    public Transform playerTrfm;
    [SerializeField] float lerpRate;

    public Transform camTrfm;
    [SerializeField] float sensitivity;

    [SerializeField] Rigidbody rb;
    [SerializeField] bool isLocal;

    [SerializeField] GroundDetect groundDetect;
    [SerializeField] HPEntity hpScript;
    [SerializeField] Collider hurtbox;
    bool dead, invuln;

    [SerializeField] PlayerUIHandler uiHandler;
    public static ushort playerObjID, teamID;

    [SerializeField] PlayerAudio playerAudio;

    public static PlayerController self;
    public Transform trfm;
    public Transform targetTrfm;
    public PositionTracker posTracker;

    private void Awake()
    {
        if (isLocal)
        {            
            self = GetComponent<PlayerController>();
        }
        trfm = transform;
        targetTrfm = trfm;
    }

    new void Start()
    {
        hpScript.OnDamage += GetComponent<PlayerController>().OnDamage;
        hpScript.useDefaultBehavior = false;
        playerTrfm.parent = null;
        SetMana(100);
        //playerObjID = objID;
        hpScript.objID = objID;
        slashHitbox.Init(objID);

        if (isLocal)
        {
            buffer20 = new byte[20];
            buffer16 = new byte[16];
            buffer7 = new byte[7];
            buffer6 = new byte[6];
            buffer4 = new byte[4];

            if (playerObjID < 64) { NetworkManager.InitializeNetObj(self, 0); }            

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            uiHandler.SetHP(100);
        }        
    }

    private void Update()
    {
        if (dead)
        {
            return;
        }

        if (isLocal)
        {
            if (Input.GetKeyDown(KeyCode.Space)) { Jump(); }
            if (UpdateKeyStatuses()) { SendTrfm(); }

            if (updateTimeout < 0) { SendTrfm(); }
            updateTimeout -= Time.deltaTime;

            HandleRotationInput();
            HandleAbilities();
            HandleDoppelSwap();
        }

        HandleTimers();
        UpdateVelocity();
    }

    #region UPDATE_CALLS

    void HandleTimers()
    {
        if (vanishInvuln > 0)
        {
            vanishInvuln -= Time.deltaTime;
            if (vanishInvuln <= 0)
            {
                EnableHurtbox(true);
                vanishInvuln = 0;
            }
        }

        if (slashTimer > 0)
        {
            slashTimer -= Time.deltaTime;
            rb.velocity = Vector3.zero;
            if (slashHitbox.col.enabled && slashTimer < 0.1f)
            {
                slashHitbox.Deactivate();
            }
            if (slashTimer <= 0)
            {
                if (isLocal)
                {
                    swordTrfm.position = swordPositions[0].position;
                    swordTrfm.rotation = swordPositions[0].rotation;
                }                
                slashFX.Stop();
            }
        }

        if (doppelSwapCD > 0)
        {
            doppelSwapCD--;
        }
    }

    void Respawn()
    {
        trfm.position = Vector3.zero + Vector3.up * 2;
        SetHP(100);
        SetMana(100);
        dead = false;
    }

    void Jump()
    {
        if (OnGround())
        {
            velocityVect = rb.velocity;
            velocityVect.y = jumpPower;
            rb.velocity = velocityVect;

            buffer6[5] = 0;
        }
        else if (mana >= 200)
        {
            velocityVect = rb.velocity;
            velocityVect.y = jumpPower * dJumpMultiplier;
            rb.velocity = velocityVect;
            AddMana(-200);

            buffer6[5] = 1;
        }

        buffer6[FUNCTION_ID] = JUMP_UPDATE;
        NetworkManager.SetBufferTime(buffer6, 3);
        NetworkManager.Send(buffer6);
    }

    #endregion

    #region ABILITIES

    [SerializeField] float dashDistance;

    byte projectileType;
    [SerializeField] GameObject[] projectiles;

    [SerializeField] GameObject clone;
    [SerializeField] MeshRenderer[] playerMeshes;

    [SerializeField] Transform firePoint;
    [SerializeField] ParticleSystem slashFX;
    [SerializeField] Hitbox slashHitbox;
    Vector3 slashVect;
    [SerializeField] Clone activeClone;
    [SerializeField] float slashScale;
    int mana;

    float slashTimer, slashRayDist;
    float vanishInvuln;

    int disableHurtbox;

    bool vanished;

    RaycastHit rayHit;
    void HandleAbilities()
    {
        if (mana > 200)
        {
            if (Input.GetMouseButtonDown(0))
            {
                AddMana(-200);

                uint eventID = Tools.RandomEventID();
                CastSlash(eventID);

                buffer7[FUNCTION_ID] = SLASH_UPDATE;
                NetworkManager.SetBufferUInt(buffer7, eventID, 3);
                NetworkManager.Send(buffer7);                
            }
            if (Input.GetMouseButtonDown(1))
            {
                uint eventID = Tools.RandomEventID();
                Instantiate(projectiles[projectileType], firePoint.position, firePoint.rotation)
                    .GetComponent<Projectile>().Init(objID, teamID, 0, HPEntity.SYNC_HP, eventID);
                playerAudio.PlayShurikenThrow();
                SendProjectileAction(eventID);
                AddMana(-200);
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                CastVanish();
                AddMana(-200);
                buffer4[FUNCTION_ID] = VANISH_UPDATE;
                buffer4[3] = 1;
                NetworkManager.Send(buffer4);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                mana += 100000;
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if (vanished) { EndVanish(); }
        }
    }

    void CastSlash(uint eventID)
    {
        //PlayerObj.position = transform.position;
        if (isLocal)
        {
            swordTrfm.position = swordPositions[1].position;
            swordTrfm.rotation = swordPositions[1].rotation;
        }

        if (Physics.Raycast(camTrfm.position, camTrfm.forward, out rayHit, dashDistance, Tools.terrainMask))
        {
            slashVect = rayHit.point;
            slashRayDist = rayHit.distance;
        }
        else
        {
            slashVect = camTrfm.position + camTrfm.forward * dashDistance;
            slashRayDist = dashDistance;
        }
        
        transform.position = playerTrfm.position + camTrfm.forward * (slashRayDist - 2) + Vector3.up * 1f;
        if (isLocal) { SendTrfm(); }

        slashHitbox.trfm.position = (slashVect + playerTrfm.position) / 2f;
        slashHitbox.trfm.rotation = camTrfm.rotation;

        slashVect = slashHitbox.trfm.localScale;
        slashVect.z = (slashRayDist + 6) * slashScale;
        slashHitbox.trfm.localScale = slashVect;
        slashHitbox.Activate(eventID);

        playerAudio.PlaySlash();
        slashFX.Play();
        slashTimer = 0.3f;
    }

    [SerializeField] GameObject swordObj;
    [SerializeField] GameObject headObj;
    [SerializeField] GameObject UIObj;
    void CastVanish()
    {
        activeClone = Instantiate(clone, trfm.position, playerTrfm.rotation).GetComponent<Clone>();
        activeClone.Init(playerTrfm.position, hpScript.HP, camTrfm.rotation, swordTrfm, sincycle);

        targetTrfm = activeClone.transform;
        posTracker.trfm = activeClone.transform;

        for (int i = 0; i < playerMeshes.Length; i++)
        {
            playerMeshes[i].enabled = false;
        }
        if (!isLocal)
        {
            swordObj.SetActive(false);
            headObj.SetActive(false);
            UIObj.SetActive(false);
        }

        EnableHurtbox(false);
        vanishInvuln = 0.5f;

        vanished = true;
    }

    void EndVanish()
    {
        if (isLocal)
        {
            buffer4[2] = 2;
            buffer4[3] = 0;
            NetworkManager.Send(buffer4);
        }
        else
        {
            swordObj.SetActive(true);
            headObj.SetActive(true);
            UIObj.SetActive(true);
        }

        for (int i = 0; i < playerMeshes.Length; i++)
        {
            playerMeshes[i].enabled = true;
        }

        if (vanishInvuln > 0.25f)
        {
            EnableHurtbox(true);
            vanishInvuln = 0.25f;
        }        

        targetTrfm = trfm;
        posTracker.trfm = trfm;

        if (activeClone) { activeClone.End(); }        
        vanished = false;
    }

    void EnableHurtbox(bool enable)
    {
        if (enable)
        {
            disableHurtbox--;
            if (disableHurtbox < 1)
            {
                disableHurtbox = 0;
                hurtbox.enabled = true;
            }
        }
        else
        {
            disableHurtbox++;
            hurtbox.enabled = false;
        }
    }

    #endregion

//  TODO...
//  Energy, movement speed

    #region NETWORKING

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);

        if (isLocal)
        {
            playerObjID = ID;
            NetworkManager.players.Add(ID, GetComponent<PlayerController>());
        }        
        hpScript.objID = ID;
        slashHitbox.Init(ID);
        //Debug.Log("player received ID: " + ID);

        if (isLocal)
        {
            NetworkManager.SetBufferUShort(buffer20, playerObjID);
            NetworkManager.SetBufferUShort(buffer16, playerObjID);
            NetworkManager.SetBufferUShort(buffer7, playerObjID);
            NetworkManager.SetBufferUShort(buffer6, playerObjID);
            NetworkManager.SetBufferUShort(buffer4, playerObjID);
        }        
    }

    public void AssignTeam(ushort pTeamID)
    {
        //buffer6[FUNCTION_ID] = TEAM_UPDATE;
        //NetworkManager.SetBufferUShort(buffer6, pTeamID, 3);
        //NetworkManager.Send(buffer6);
    }

    public override void OnDcd()
    {
        Destroy(playerTrfm.gameObject);
        Destroy(gameObject);
    }

    float updateTimeout;
    bool keyFwd = false, keyBack = false, keyLeft = false, keyRight = false;

    int temp;
    byte[] buffer20; //projectiles
    byte[] buffer16; //trfm
    byte[] buffer7; //cast slash
    byte[] buffer6; //jump, set team
    byte[] buffer4; //vanish

    public void SendProjectileAction(uint eventID)
    {
        if (!IsConnected()) { return; }

        buffer20[FUNCTION_ID] = PROJECTILE_UPDATE;
        buffer20[3] = projectileType;
        NetworkManager.SetBufferTime(buffer20, 4);
        NetworkManager.SetBufferCoords(buffer20, playerTrfm.position, 6);

        NetworkManager.SetBufferUShort(buffer20, NetworkManager.EncodePosValue(playerTrfm.eulerAngles.y), 12);
        NetworkManager.SetBufferUShort(buffer20, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 14);

        NetworkManager.SetBufferUInt(buffer20, eventID, 16);

        NetworkManager.Send(buffer20);
    }
    public void SendTrfm()
    {
        updateTimeout = 0.2f;
        if (!IsConnected()) { return; }
        //NetworkManager.SetBufferUShort(buffer16, playerObjID);
        buffer16[2] = 0;
        NetworkManager.SetBufferTime(buffer16, 3);

        NetworkManager.SetBufferCoords(buffer16, trfm.position);

        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(playerTrfm.eulerAngles.y), 11);
        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(camTrfm.localEulerAngles.x), 13);

        temp = keyFwd ? 8 : 0;
        temp += keyBack ? 4 : 0;
        temp += keyLeft ? 2 : 0;
        temp += keyRight ? 1 : 0;

        buffer16[15] = (byte)temp;
    
        NetworkManager.Send(buffer16);
    }

    const int FUNCTION_ID = 2;
    const int TRFM_UPDATE = 0, PROJECTILE_UPDATE = 1, VANISH_UPDATE = 2, SLASH_UPDATE = 3, JUMP_UPDATE = 4, 
        TEAM_UPDATE = 5, HP_UPDATE = 223;
    public override void NetworkUpdate(byte[] buffer)
    {        
        if (buffer[FUNCTION_ID] == HP_UPDATE)
        {
            hpScript.HandleSync(buffer);
        }

        if (buffer[FUNCTION_ID] == TRFM_UPDATE) { HandleTrfmUpdate(buffer); }

        else if (buffer[FUNCTION_ID] == PROJECTILE_UPDATE)
        {
            AddMana(-200);
            playerAudio.PlayShurikenThrow();
            playerTrfm.position = NetworkManager.GetBufferCoords(buffer, 6);
            SetRotation(NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 14)), NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 12)));

            Instantiate(projectiles[buffer[3]], firePoint.position, firePoint.rotation)
                .GetComponent<Projectile>().Init(objID, teamID, NetworkManager.GetBufferDelta(buffer, 4), HPEntity.DONT_SYNC, NetworkManager.GetBufferUInt(buffer, 16));
        }

        else if (buffer[FUNCTION_ID] == VANISH_UPDATE)
        {
            AddMana(-200);
            if (buffer[3] == 1) { CastVanish(); }
            else { EndVanish(); }
        }

        else if (buffer[FUNCTION_ID] == SLASH_UPDATE) {
            CastSlash(NetworkManager.GetBufferUInt(buffer, 3));
        }

        else if (buffer[FUNCTION_ID] == JUMP_UPDATE)
        {
            velocityVect = rb.velocity;
            velocityVect.y = jumpPower;

            if (buffer[5] == 1)
            {
                velocityVect.y *= dJumpMultiplier;
                AddMana(-200);
            }
            rb.velocity = velocityVect;
        } 

        else if (buffer[FUNCTION_ID] == TEAM_UPDATE)
        {
            teamID = NetworkManager.GetBufferUShort(buffer, 3);
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

        anyMovementKey = keyFwd || keyBack || keyLeft || keyRight;

        Update();

        //player dead reckoning:
        //transform.position += rb.velocity * NetworkManager.GetBufferDelay(buffer, 2);
    }

    #endregion

    #region HANDLE_INPUT

    bool anyMovementKey;
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

        anyMovementKey = keyFwd || keyBack || keyRight || keyLeft;

        return inputUpdate;
    }

    void HandleRotationInput()
    {
        yMouse = -Input.GetAxis("MY") * sensitivity;      //right now camera is flipped, and rotates indefintley, so fix that and set to max 90 degrees
        xMouse = Input.GetAxis("MX") * sensitivity;

        camTrfm.Rotate(Vector3.right * yMouse);
        playerTrfm.Rotate(Vector3.up * xMouse);

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

        velocityVect = playerTrfm.forward * velocityVect.z + playerTrfm.right * velocityVect.x;
        velocityVect.y = rb.velocity.y;
        rb.velocity = velocityVect;
    }

    [SerializeField] float yMouse, xMouse;

    #region EXTERNAL_SCRIPTS

    bool OnGround()
    {
        return groundDetect.touchCount > 0;
    }

    int calcHP;
    public void OnDamage(ushort amount, ushort sourceID)
    {
        hpScript.damageFX.Play();
        hpScript.damageSFX.Play();
        calcHP = hpScript.HP - amount;

        if (calcHP < 0.01)
        {            
            Debug.Log("lethal");
            SetHP(0);
            hpScript.SyncHP(HPEntity.SYNC_HP, Tools.RandomEventID());
            HandleDeath();
        }
        else
        {
            SetHP((ushort)calcHP);
        }
    }

    [SerializeField] SpriteRenderer endText;
    [SerializeField] Sprite winText, loseText;
    public int gamemode;
    public const int GM_FFA = 0, GM_TDM = 1;
    void HandleDeath()
    {
        if (gamemode == GM_FFA)
        {
            SceneManager.LoadScene("FFA");
            Respawn();
        }
        if (gamemode == GM_TDM)
        {
            dead = true;
            for (int i = 0; i < doppels.Length; i++)
            {
                if (doppels[i])
                {
                    SetHP(doppels[i].hpScript.HP);
                    DoppelSwap(i);
                    doppels[i].hpScript.TakeDamage(9999, 0, 0, HPEntity.SYNC_HP, Tools.RandomEventID());
                    doppels[i].hpScript.End();
                    dead = false;
                    break;
                }
            }
            
            if (dead)
            {
                if (isLocal)
                {
                    endText.sprite = loseText;
                }
                else
                {
                    self.endText.sprite = winText;
                }
                Respawn();
            }
        }
    }

    #endregion

    #region DOPPELS

    public static List<PositionTracker> opponents = new List<PositionTracker>();
    [SerializeField] Doppel[] doppels;

    float doppelSwapCD;
    Vector3 doppelDest;

    public void AssignDoppels(Doppel[] pDoppels)
    {
        doppels = pDoppels;
    }

    void HandleDoppelSwap()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { DoppelSwap(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { DoppelSwap(1); }
        //if (Input.GetKeyDown(KeyCode.Alpha3)) { DoppelSwap(3); }
    }

    void DoppelSwap(int index)
    {
        if (doppelSwapCD < 0.01f)
        {
            doppelDest = trfm.position;
            trfm.position = doppels[index].trfm.position;
            SetRotation(doppels[index].head.eulerAngles.x, doppels[index].objTrfm.eulerAngles.y);
            doppels[index].trfm.position = doppelDest;            
            doppels[index].SyncTrfm(true);

            doppelSwapCD = 1;
        }        
    }

    #endregion

    #region ANIMATIONS

    [SerializeField] float targetYRot, targetXRot;
    Vector3 eulerAngles;
    [SerializeReference] TextMeshProUGUI tmp;
    void FixedUpdate()
    {
        if (slashTimer > 0)
        {
            rb.velocity = Vector3.zero;
        }

        playerTrfm.position += (transform.position - playerTrfm.position) * lerpRate;     

        if (!isLocal)
        {
            SetRotation(Tools.RotationalLerp(camTrfm.localEulerAngles.x, targetXRot, 0.3f), Tools.RotationalLerp(playerTrfm.localEulerAngles.y, targetYRot, 0.5f));
        }

        if (vanished)
        {
            if (!activeClone)
            {
                EndVanish();
            }

            AddMana(-2);
            if (mana < 1 && isLocal)
            {
                EndVanish();
                SetMana(0);
            }
        }
        else
        {
            if (mana < 1000)
            {
                AddMana(2);
                if (mana > 1000)
                {
                    SetMana(1000);
                }
            }
        }

        HandleSwordBobbing();
        HandleRunSFX();
    }

    [SerializeField] Transform[] swordPositions; //0: default, 1: slash
    [SerializeField] float bobRate, bobStrength, yBobMultiplier, viewBobMultiplier;
    [SerializeField] Transform swordTrfm;
    float effectiveBobStrength;
    float sincycle; float sin;
    Vector3 swordVect;
    void HandleSwordBobbing()
    {
        if (slashTimer > 0) { return; }
        if (anyMovementKey && OnGround())
        {
            sincycle += bobRate;
            if (effectiveBobStrength < bobStrength)
            {
                effectiveBobStrength += bobStrength * 0.1f;
                if (effectiveBobStrength > bobStrength)
                {
                    effectiveBobStrength = bobStrength * 0.1f;
                }
            }

            HandleViewBobbing();
        }
        else
        {
            sincycle += bobRate * 0.07f;
            if (effectiveBobStrength > bobStrength * 0.1f)
            {
                effectiveBobStrength -= bobStrength * 0.03f;
                if (effectiveBobStrength < bobStrength * 0.5f)
                {
                    effectiveBobStrength = bobStrength * 0.5f;
                }
            }
        }
        sin = Mathf.Sin(sincycle);

        swordVect = swordTrfm.localEulerAngles;
        swordVect.x = sin * bobStrength - 18;
        swordTrfm.localEulerAngles = swordVect;

        swordVect = swordTrfm.localPosition;
        swordVect.y = -sin * bobStrength * yBobMultiplier;
        swordVect.z = -sin * bobStrength * yBobMultiplier * 2;
        swordTrfm.localPosition = swordVect;
    }

    void HandleViewBobbing()
    {
        swordVect = camTrfm.localEulerAngles;
        swordVect.x += sin * viewBobMultiplier;
        camTrfm.localEulerAngles = swordVect;
    }

    bool playRunSFX;
    void HandleRunSFX()
    {
        playerAudio.ToggleRun(anyMovementKey && OnGround() && !vanished);
    }

    #endregion

    #region HELPERS

    void SetHP(int value, bool sync = true)
    {
        uiHandler.SetHP(value);
        hpScript.HP = value;
        //if (sync) { hpScript.SyncHP(); }        
    }

    void SetMana(int value)
    {
        mana = value;
        if (isLocal) { uiHandler.SetMana(mana); }
    }

    void AddMana(int amount)
    {
        mana += amount;
        if (isLocal) { uiHandler.SetMana(mana); }
    }

    void SetRotation(float x, float y)
    {
        eulerAngles = playerTrfm.eulerAngles;
        eulerAngles.y = y;
        playerTrfm.eulerAngles = eulerAngles;

        eulerAngles = camTrfm.localEulerAngles;
        eulerAngles.x = x;
        camTrfm.localEulerAngles = eulerAngles;
    }

    #endregion
}
