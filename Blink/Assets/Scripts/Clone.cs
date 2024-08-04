using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : MonoBehaviour
{
    [SerializeField] Transform meshObj, camTrfm, swordTrfm, emptyTrfm;
    [SerializeField] HPEntity hpScript;
    [SerializeField] UIHandler uiHandler;
    [SerializeField] Billboard billboard;

    [SerializeField] List<PositionTracker> opponents = new List<PositionTracker>();
    ushort ownerObjID;
    public void Init(Vector3 playerPos, int HP, Quaternion camRot, Transform pSwordTrfm, float pSinCycle, List<PositionTracker> pOpponents, ushort pOwnerObjID)
    {
        meshObj.position = playerPos;
        hpScript.HP = HP;
        uiHandler.SetHP(HP);
        billboard.Update();

        camTrfm.rotation = camRot;
        swordTrfm.position = pSwordTrfm.position;
        swordTrfm.rotation = pSwordTrfm.rotation;
        sincycle = pSinCycle;
        ownerObjID = pOwnerObjID;

        SetTarget();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        meshObj.position += (transform.position - meshObj.position) * 0.2f;
        HandleFacing();
        HandleSwordBobbing();
    }

    public void End()
    {
        hpScript.End();
    }

    void HandleFacing()
    {
        if (hasTarget)
        {
            emptyTrfm.forward = target.PredictedPosition(0.3f) - camTrfm.position;
            SetRotation(Tools.RotationalLerp(camTrfm.eulerAngles.x, emptyTrfm.eulerAngles.x, 0.2f), Tools.RotationalLerp(meshObj.eulerAngles.y, emptyTrfm.eulerAngles.y, 0.2f));
        }
    }

    Vector3 eulerAngles;
    void SetRotation(float x, float y)
    {
        eulerAngles = meshObj.eulerAngles;
        eulerAngles.y = y;
        meshObj.eulerAngles = eulerAngles;

        eulerAngles = camTrfm.localEulerAngles;
        eulerAngles.x = x;
        camTrfm.localEulerAngles = eulerAngles;
    }

    [SerializeField] float bobRate, bobStrength, yBobMultiplier;
    float effectiveBobStrength;
    float sincycle; float sin;
    Vector3 swordVect;

    void HandleSwordBobbing()
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

        sin = Mathf.Sin(sincycle);

        swordVect = swordTrfm.localEulerAngles;
        swordVect.x = sin * bobStrength - 18;
        swordTrfm.localEulerAngles = swordVect;

        swordVect = swordTrfm.localPosition;
        swordVect.y = -sin * bobStrength * yBobMultiplier;
        swordVect.z = -sin * bobStrength * yBobMultiplier * 2;
        swordTrfm.localPosition = swordVect;
    }

    bool hasTarget = false;
    PositionTracker target;
    void SetTarget()
    {
        if (opponents.Count > 0)
        {
            opponents.Sort((a, b) => Vector3.SqrMagnitude(meshObj.position - a.trfm.position).CompareTo(Vector3.SqrMagnitude(meshObj.position - b.trfm.position)));
            for (int i = 0; i < opponents.Count; i++)
            {
                if (!Physics.Linecast(meshObj.position, opponents[i].trfm.position, Tools.terrainMask))
                {
                    hasTarget = true;
                    target = opponents[i];
                    break;
                }
            }
        }
        else
        {
            foreach (ushort objID in NetworkManager.players.Keys)
            {
                if (objID != ownerObjID
                    && !Physics.Linecast(meshObj.position, NetworkManager.players[objID].trfm.position, Tools.terrainMask))
                {
                    target = NetworkManager.players[objID].posTracker;
                    hasTarget = true;
                }
            }
        }    
    }
}
