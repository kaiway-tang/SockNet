using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : MonoBehaviour
{
    [SerializeField] Transform meshObj, camTrfm, swordTrfm;
    [SerializeField] HPEntity hpScript;
    [SerializeField] UIHandler uiHandler;
    [SerializeField] Billboard billboard;

    public void Init(Vector3 playerPos, int HP, Quaternion camRot, Transform pSwordTrfm, float pSinCycle)
    {
        meshObj.position = playerPos;
        hpScript.HP = HP;
        uiHandler.SetHP(HP);
        billboard.Update();

        camTrfm.rotation = camRot;
        swordTrfm.position = pSwordTrfm.position;
        swordTrfm.rotation = pSwordTrfm.rotation;
        sincycle = pSinCycle;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        meshObj.position += (transform.position - meshObj.position) * 0.2f;

        HandleSwordBobbing();
    }

    public void End()
    {
        hpScript.End();
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
}
