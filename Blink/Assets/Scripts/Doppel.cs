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

    [SerializeField] GameObject beacon;
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

    new void Start()
    {
        base.Start();        

        hpScript.OnDamage += GetComponent<Doppel>().OnDamage;
    }  
    public void Init(ushort pTeamID, PlayerController pOwner, List<PositionTracker> pOpponents, bool pIsLocal)
    {
        objTrfm.parent = null;
        teamID = pTeamID;
        hpScript.teamID = pTeamID;
        playerOwner = pOwner;

        opponents = pOpponents;
        isLocal = pIsLocal;
        if (!isLocal) { shuriken = marker; }

        missingAnchorPos = 3;
    }

    #region NETWORKING

    public override void AssignObjID(ushort ID)
    {
        base.AssignObjID(ID);
        hpScript.objID = ID;
        slashHitbox.Init(ID);

        NetworkManager.SetBufferUShort(buffer16, ID);
        NetworkManager.SetBufferUShort(buffer15, ID);
        NetworkManager.SetBufferUShort(buffer9, ID);
        NetworkManager.SetBufferUShort(buffer7, ID);
        NetworkManager.SetBufferUShort(buffer4, ID);

        if (isLocal) { SyncEventID(); }
    }

    byte[] buffer16 = new byte[16];
    byte[] buffer15 = new byte[15];
    byte[] buffer9 = new byte[9];
    byte[] buffer7 = new byte[7];
    byte[] buffer4 = new byte[4];
    const int FUNCTION_ID = 2;
    const int TRFM_UPDATE = 0,
        EVENT_ID_UPDATE = 1, SET_TARGET = 2, SET_ANCHOR_POS = 3, SET_STRAFE_POINTS = 4, SET_STRAFE_STATE = 5,
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
            if (buffer[3] == 255)
            { 
                hasTarget = false;
            }
            else
            {
                if (!hasTarget)
                {
                    //NOTE: play horn warning
                    if (attackTimer < 0.4f) { attackTimer = 0.4f; }
                    targettingTimer = 25;
                }

                hasTarget = true;
                target = opponents[buffer[3]];
            }
        }

        if (buffer[FUNCTION_ID] == SET_ANCHOR_POS) { anchorPos = NetworkManager.GetBufferCoords(buffer, 3); }

        if (buffer[FUNCTION_ID] == SET_STRAFE_POINTS)
        {
            leftStrafePoint = NetworkManager.GetBufferCoords(buffer, 3);
            rightStrafePoint = NetworkManager.GetBufferCoords(buffer, 9);
        }

        if (buffer[FUNCTION_ID] == SET_STRAFE_STATE) { strafeToLeft = buffer[3] == 1; }
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
        buffer4[FUNCTION_ID] = SET_STRAFE_STATE;
        buffer4[3] = (byte)(strafeToLeft ? 1 : 0);
        NetworkManager.Send(buffer4);
    }

    #endregion
    public void OnDamage(ushort amount, ushort sourceID)
    {
        //hpScript.SyncHP();
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

        objTrfm.position += (trfm.position - objTrfm.position) * 0.2f;
        if (mana < 500) { mana++; }
    }

    float targetDistance, predictTime;
    float attackTimer;
    uint eventID;

    void HandleAttacking()
    {
        if (hasTarget)
        {
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }
            else if (mana >= 100)
            {
                attackTimer = 1;
                if (targetDistance < 12.5f)
                {
                    CastSlash(eventID);
                }
                else
                {
                    Instantiate(shuriken, firepoint.position, firepoint.rotation).GetComponent<Shuriken>().Init(0, teamID, 0, HPEntity.SYNC_HP, eventID);
                }
                if (isLocal) { SyncEventID(); }
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
            if (targetDistance < 12.5f) { predictedPos.y = target.trfm.position.y; }            
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
            if (leftDetect.touchCount > 0 && rightDetect.touchCount > 0)
            {                
                missingAnchorPos--;
                if (missingAnchorPos < 1)
                {
                    anchorPos = trfm.position;
                    if (isLocal) { SyncAnchor(); }
                }
                return;
            }
            else
            {
                missingAnchorPos = 3;
                return;
            }
        }
        if (hasTarget)
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

    float minSqrDist, tempSqrDist;
    int targettingTimer;
    void HandleTargetting()
    {   
        if (!isLocal) { return; }
        if (targettingTimer > 0)
        {
            targettingTimer -= 1;
        }
        else
        {
            if (hasTarget)
            {
                rb.velocity = Vector3.zero;
                SyncTrfm(false);
            }
            hasTarget = false;
            targettingTimer = 50;

            byte index = 255;
            minSqrDist = 99999;
            for (int i = opponents.Count - 1; i >= 0; i--)
            {
                if (!opponents[i]) { opponents.RemoveAt(i); continue; }
                if (!Physics.Linecast(head.position, opponents[i].trfm.position, Tools.terrainMask))
                {
                    tempSqrDist = Vector3.SqrMagnitude(objTrfm.position - opponents[i].trfm.position);
                    if (tempSqrDist < minSqrDist)
                    {
                        minSqrDist = tempSqrDist;
                        target = opponents[i];
                        index = (byte)i;

                        if (!hasTarget)
                        {
                            //NOTE: play horn warning
                            if (attackTimer < 0.4f) { attackTimer = 0.4f; }
                            hasTarget = true;
                            targettingTimer = 25;
                        }
                    }
                }
            }
            if (hasTarget)
            {
                SetStrafePoints();
                SyncStrafePoints();
            }
            SyncTarget(index);
        }        
    }

    void SyncTarget(byte index)
    {
        buffer4[FUNCTION_ID] = SET_TARGET;
        buffer4[3] = index;
        NetworkManager.Send(buffer4);
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
