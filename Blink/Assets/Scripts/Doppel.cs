using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doppel : NetworkObject
{
    public Transform trfm, objTrfm, head, firepoint;
    [SerializeField] GameObject shuriken, marker;
    [SerializeField] Hitbox slashHitbox;
    [SerializeField] List<PositionTracker> opponents = new List<PositionTracker>();

    [SerializeField] float movementSpeed, predictionMultiplier, turnLerpRate;

    [SerializeField] Vector3 anchorPos;
    public int missingAnchorPos;
    [SerializeField] float strafeDistance;
    Vector3 leftStrafePoint, rightStrafePoint;
    bool strafeToLeft;

    public GameObject beacon;
    public HPEntity hpScript;

    public PositionTracker posTracker, target;
    [SerializeField] bool hasTarget;
    int mana;

    public PlayerController playerOwner;
    public ushort teamID;
    [SerializeField] Transform emptyTrfm;
    [SerializeField] Rigidbody rb;
    [SerializeField] GroundDetect leftDetect, rightDetect;

    [SerializeField] PlayerAudio playerAudio;
    [SerializeField] ParticleSystem slashFX;

    public bool isLocal;
    int doppelNumber;

    new void Start()
    {
        base.Start();        

        hpScript.OnDamage += GetComponent<Doppel>().OnDamage;
    }  
    public void Init(ushort pTeamID, PlayerController pOwner, List<PositionTracker> pOpponents, bool pIsLocal, int pDoppelNumber)
    {
        objTrfm.parent = null;
        teamID = pTeamID;
        hpScript.teamID = pTeamID;
        slashHitbox.Init(objID, teamID);
        playerOwner = pOwner;

        opponents = pOpponents;
        isLocal = pIsLocal;
        if (!isLocal) { shuriken = marker; }

        missingAnchorPos = 3;

        doppelNumber = pDoppelNumber;
    }

    #region NETWORKING

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);
        hpScript.AssignObjID(ID);        
        posTracker.objID = ID;

        NetworkManager.SetBufferUShort(buffer16, ID);
        NetworkManager.SetBufferUShort(buffer15, ID);
        NetworkManager.SetBufferUShort(buffer9, ID);
        NetworkManager.SetBufferUShort(buffer7, ID);
        NetworkManager.SetBufferUShort(buffer5, ID);

        if (isLocal) {
            SyncEventID();
            attackTimer = 0.5f;
            SyncAttackTimer();
        }
    }

    byte[] buffer16 = new byte[16];
    byte[] buffer15 = new byte[15];
    byte[] buffer9 = new byte[9];
    byte[] buffer7 = new byte[7];
    byte[] buffer5 = new byte[5];
    const int FUNCTION_ID = 2;
    const int TRFM_UPDATE = 0,
        EVENT_ID_UPDATE = 1, SET_TARGET = 2, SET_ANCHOR_POS = 3, SET_STRAFE_POINTS = 4, SET_STRAFE_STATE = 5, SET_ATK_TIMER = 6,
        FIRE_SHURIKEN = 7,
        HP_UPDATE = 223;
    public override void NetworkUpdate(byte[] buffer)
    {
        if (buffer[FUNCTION_ID] == HP_UPDATE) { hpScript.HandleSync(buffer); }        

        if (hpScript.HP <= 0) { return; }

        if (buffer[FUNCTION_ID] == TRFM_UPDATE)
        {
            if (!trfm) { return; }
            trfm.position = NetworkManager.GetBufferCoords(buffer, 5);
            SetRotation(NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer,13)),
                NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 11)));

            if (buffer[15] == 1) { missingAnchorPos = 3; }            
        }

        if (buffer[FUNCTION_ID] == EVENT_ID_UPDATE) { eventID = NetworkManager.GetBufferUInt(buffer, 3); }

        if (buffer[FUNCTION_ID] == SET_TARGET)
        {
            ushort targetObjID = NetworkManager.GetBufferUShort(buffer, 3);
            if (targetObjID == 0)
            { 
                hasTarget = false;
            }
            else
            {
                if (!hasTarget)
                {
                    if (combatTimer < 250)
                    {
                        if (combatTimer < 100) { PlayerController.self.playerAudio.PlayDrumTap(doppelNumber); }
                        combatTimer = 250;
                    }                    
                    if (attackTimer < 0.4f) { attackTimer = 0.4f; }
                    targettingTimer = 25;
                }

                hasTarget = true;                
                for (int i = 0; i < opponents.Count; i++)
                {
                    if (opponents[i].objID == targetObjID) { target = opponents[i]; }
                }
            }
        }

        if (buffer[FUNCTION_ID] == SET_ANCHOR_POS) { anchorPos = NetworkManager.GetBufferCoords(buffer, 3); }

        if (buffer[FUNCTION_ID] == SET_STRAFE_POINTS)
        {
            leftStrafePoint = NetworkManager.GetBufferCoords(buffer, 3);
            rightStrafePoint = NetworkManager.GetBufferCoords(buffer, 9);
        }

        if (buffer[FUNCTION_ID] == SET_STRAFE_STATE) { strafeToLeft = buffer[3] == 1; }

        if (buffer[FUNCTION_ID] == SET_ATK_TIMER)
        {
            attackTimer = NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 3))
                + NetworkManager.GetBufferDelta(buffer, 5);
        }

        if (buffer[FUNCTION_ID] == FIRE_SHURIKEN)
        {
            SetRotation(NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 14)),
                NetworkManager.DecodePosValue(NetworkManager.GetBufferUShort(buffer, 12)));
            Instantiate(shuriken, NetworkManager.GetBufferCoords(buffer, 6), firepoint.rotation)
                .GetComponent<Projectile>().Init(objID, teamID, NetworkManager.GetBufferDelta(buffer, 4), HPEntity.SYNC_HP, eventID);
            eventIDUsed = true;
        }
    }

    public void SyncTrfm(bool resetAnchorPos)
    {
        buffer16[FUNCTION_ID] = TRFM_UPDATE;
        NetworkManager.SetBufferTime(buffer16, 3);
        NetworkManager.SetBufferCoords(buffer16, trfm.position, 5);
        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(objTrfm.eulerAngles.y), 11);
        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(head.localEulerAngles.x), 13);
        buffer16[15] = (byte)(resetAnchorPos ? 1 : 0);
        NetworkManager.Send(buffer16);

        if (resetAnchorPos) { missingAnchorPos = 3; }
    }

    void SyncEventID()
    {
        buffer7[FUNCTION_ID] = EVENT_ID_UPDATE;
        eventID = Tools.RandomEventID();
        NetworkManager.SetBufferUInt(buffer7, eventID, 3);
        NetworkManager.Send(buffer7);
    }

    void SyncAnchor()
    {
        buffer9[FUNCTION_ID] = SET_ANCHOR_POS;
        NetworkManager.SetBufferCoords(buffer9, anchorPos, 3);
        NetworkManager.Send(buffer9);
    }

    void SyncStrafePoints()
    {
        buffer15[FUNCTION_ID] = SET_STRAFE_POINTS;
        NetworkManager.SetBufferCoords(buffer15, leftStrafePoint, 3);
        NetworkManager.SetBufferCoords(buffer15, rightStrafePoint, 9);
        NetworkManager.Send(buffer15);
    }

    void SyncStrafeDirection()
    {
        buffer5[FUNCTION_ID] = SET_STRAFE_STATE;
        buffer5[3] = (byte)(strafeToLeft ? 1 : 0);
        NetworkManager.Send(buffer5);
    }

    void SyncAttackTimer()
    {
        buffer7[FUNCTION_ID] = SET_ATK_TIMER;
        NetworkManager.SetBufferUShort(buffer7, NetworkManager.EncodePosValue(attackTimer), 3);
        NetworkManager.SetBufferTime(buffer7, 5);        
    }

    #endregion

    int combatTimer;
    public void OnDamage(ushort amount, ushort sourceID)
    {
        combatTimer = 1000;
    }

    private void Update()
    {
        HandleAttacking();
    }

    void FixedUpdate()
    {
        if (hasTarget && !target)
        {
            hasTarget = false;
            for (int i = opponents.Count - 1; i >= 0; i--)
            {
                if (!opponents[i])
                {
                    opponents.RemoveAt(i);
                }
            }
        }

        HandleTargetting();
        HandleFacing();
        HandleStrafing();
        //HandleBeacon();

        if (slashTimer > 0)
        {
            slashTimer -= Time.deltaTime;
            rb.velocity = Vector3.zero;
            if (slashTimer <= 0)
            {
                slashFX.Stop();
                slashHitbox.Deactivate();
            }
        }

        if (combatTimer > 0) { combatTimer--; }

        objTrfm.position += (trfm.position - objTrfm.position) * 0.2f;
        if (mana < 500) { mana++; }
    }

    float targetDistance, predictTime;
    float attackTimer;
    uint eventID; bool eventIDUsed;

    void HandleAttacking()
    {
        if (hasTarget)
        {
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
                if (isLocal && eventIDUsed && attackTimer < 0.4f)
                {
                    SyncEventID();
                    eventIDUsed = false;
                }
            }
            else if (mana >= 100)
            {
                attackTimer = 1;
                SyncAttackTimer();
                if (targetDistance < 12.5f)
                {
                    CastSlash(eventID);
                    eventIDUsed = true;
                }
                else
                {
                    if (isLocal)
                    {
                        Instantiate(shuriken, firepoint.position, firepoint.rotation).GetComponent<Shuriken>().Init(0, teamID, 0, HPEntity.SYNC_HP, eventID);

                        buffer16[FUNCTION_ID] = FIRE_SHURIKEN;
                        NetworkManager.SetBufferTime(buffer16, 4);
                        NetworkManager.SetBufferCoords(buffer16, firepoint.position, 6);
                        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(objTrfm.eulerAngles.y), 12);
                        NetworkManager.SetBufferUShort(buffer16, NetworkManager.EncodePosValue(head.eulerAngles.x), 14);
                        NetworkManager.Send(buffer16);

                        eventIDUsed = true;
                    }                    
                }                
                mana -= 100;
            }
        }        
    }

    Vector3 predictedPos;
    void HandleFacing()
    {
        if (hasTarget)
        {
            targetDistance = Vector3.Distance(trfm.position, target.trfm.position);
            predictTime = targetDistance * predictionMultiplier;
            if (predictTime > 0.6f) { predictTime = 0.6f; }            
            predictedPos = target.PredictedPosition(predictTime);
            predictedPos.y = target.trfm.position.y + 0.2f;
            emptyTrfm.forward = predictedPos - head.position;
            //emptyTrfm.forward = target.PredictedPosition(targetDistance * predictionMultiplier) - head.position;
            SetRotation(Tools.RotationalLerp(head.eulerAngles.x, emptyTrfm.eulerAngles.x, turnLerpRate), Tools.RotationalLerp(objTrfm.eulerAngles.y, emptyTrfm.eulerAngles.y, turnLerpRate));            
        }
    }

    float slashTimer;
    float slashRayDist;
    Vector3 slashVect;
    RaycastHit rayHit;
    void CastSlash(uint eventID)
    {
        if (Physics.Raycast(head.position, head.forward, out rayHit, 14, Tools.terrainMask))
        {
            slashVect = rayHit.point;
            slashRayDist = rayHit.distance;
        }
        else
        {
            slashVect = head.position + head.forward * 14;
            slashRayDist = 14;
        }

        transform.position = objTrfm.position + head.forward * (slashRayDist - 2) + Vector3.up * 1f;
        missingAnchorPos = 3;
        
        slashHitbox.trfm.position = (slashVect + objTrfm.position) / 2f;
        slashHitbox.trfm.rotation = head.rotation;

        slashVect = slashHitbox.trfm.localScale;
        slashVect.z = (slashRayDist + 6);
        slashHitbox.trfm.localScale = slashVect;
        slashHitbox.Activate(eventID);

        playerAudio.PlaySlash();
        slashFX.Play();
        slashTimer = 0.3f;
    }

    bool stationary;
    void HandleStrafing()
    {
        if (missingAnchorPos > 0)
        {
            if (Physics.Raycast(trfm.position, Vector3.down, out rayHit, 16, Tools.terrainMask))
            {
                anchorPos = rayHit.point + Vector3.up * 0.75f;
                missingAnchorPos = 0;
                if (isLocal) { SyncAnchor(); }
            }
            else
            {
                return;
            }
        }
        if (hasTarget || combatTimer > 0)
        {
            if (strafeToLeft)
            {
                if (leftDetect.touchCount < 1) { strafeToLeft = false; rb.velocity = Vector3.zero; }
                rb.velocity = Vector3.Normalize(leftStrafePoint - trfm.position) * movementSpeed;
                if (Vector3.SqrMagnitude(leftStrafePoint - trfm.position) < 1)
                {
                    strafeToLeft = false;
                    if (isLocal) { SyncStrafeDirection(); }                    
                }
            }
            else
            {
                if (rightDetect.touchCount < 1) { strafeToLeft = true; rb.velocity = Vector3.zero; }
                rb.velocity = Vector3.Normalize(rightStrafePoint - trfm.position) * movementSpeed;
                if (Vector3.SqrMagnitude(rightStrafePoint - trfm.position) < 1)
                {
                    strafeToLeft = true;
                    if (isLocal) { SyncStrafeDirection(); }
                }
            }
        }
        else
        {            
            if (Vector3.SqrMagnitude(anchorPos - trfm.position) > 1)
            {
                stationary = false;
                rb.velocity = Vector3.Normalize(anchorPos - trfm.position) * movementSpeed;                
            }
            else if (!stationary)
            {
                stationary = true;
                rb.velocity = Vector3.zero;
            }
        }
    }

    int targettingTimer;
    void HandleTargetting()
    {
        if (targettingTimer > 0)
        {
            targettingTimer -= 1;
        }
        else
        {
            if (hasTarget)
            {
                rb.velocity = Vector3.zero;
                if (isLocal) { SyncTrfm(false); }                
            }
            hasTarget = false;
            targettingTimer = 50;

            byte index = 255;

            for (int i = opponents.Count - 1; i >= 0; i--)
            {
                if (!opponents[i]) { opponents.RemoveAt(i); continue; }
            }

            opponents.Sort((b, a) => Vector3.SqrMagnitude(trfm.position - a.trfm.position).CompareTo(Vector3.SqrMagnitude(trfm.position - b.trfm.position)));

            for (int i = 0; i < opponents.Count; i++)
            {
                if (!Physics.Linecast(head.position, opponents[i].trfm.position + Vector3.up * 0.5f, Tools.terrainMask))
                {
                    target = opponents[i];
                    index = (byte)i;

                    if (!hasTarget)
                    {
                        if (combatTimer < 100) { PlayerController.self.playerAudio.PlayDrumTap(doppelNumber); }
                        if (combatTimer < 250) { combatTimer = 250; }
                        if (combatTimer < 1)
                        {
                            //PlayerController.self.playerAudio.PlayDrumTap(doppelNumber);
                            if (attackTimer < 0.4f)
                            {
                                attackTimer = 0.4f;
                                SyncAttackTimer();
                            }
                        }                        
                        hasTarget = true;
                        targettingTimer = 25;
                    }
                }
            }

            if (hasTarget)
            {
                SetStrafePoints();
                if (isLocal) { SyncStrafePoints(); }                
            }
            SyncTarget(index);
        }        
    }

    void SyncTarget(byte index)
    {
        buffer5[FUNCTION_ID] = SET_TARGET;
        if (index == 255) { NetworkManager.SetBufferUShort(buffer5, 0, 3); }
        else { NetworkManager.SetBufferUShort(buffer5, opponents[index].objID, 3); }
        NetworkManager.Send(buffer5);
    }

    void SetStrafePoints()
    {
        trfm.forward = target.trfm.position - anchorPos;
        leftStrafePoint = anchorPos + trfm.right * -strafeDistance;
        rightStrafePoint = anchorPos + trfm.right * strafeDistance;
    }

    Vector3 eulerAngles;
    void SetRotation(float x, float y)
    {
        eulerAngles = objTrfm.eulerAngles;
        eulerAngles.y = y;
        objTrfm.eulerAngles = eulerAngles;

        eulerAngles = head.localEulerAngles;
        eulerAngles.x = x;
        head.localEulerAngles = eulerAngles;
    }

    bool beaconActive;
    void HandleBeacon()
    {
        beaconActive = Vector3.SqrMagnitude(trfm.position - playerOwner.trfm.position) > 900;
        if (beacon.activeSelf != beaconActive)
        {
            beacon.SetActive(beaconActive);
        }
    }
}

