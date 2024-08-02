using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float range;
    [SerializeField] protected PlayerController targetPlayer;
    protected Transform trfm;
    // Start is called before the first frame update
    protected void Start()
    {
        trfm = transform;
    }

    
    protected void FixedUpdate()
    {
        HandleTargetable();
    }

    [SerializeField] int targetCheckTimer;
    bool lastPlayerTargetable;
    [SerializeField] protected bool isPlayerTargetable;
    void HandleTargetable()
    {
        if (targetCheckTimer > 0)
        {
            targetCheckTimer--;
        }
        else
        {
            isPlayerTargetable = Tools.BoxDist(GetTargetTrfm().position, trfm.position) < range
                                && Vector3.Distance(GetTargetTrfm().position, trfm.position) < range
                                && !Physics.Linecast(trfm.position, GetTargetTrfm().position, Tools.terrainMask);

            if (isPlayerTargetable && !lastPlayerTargetable)
            {                
                OnPlayerBecameTargetable();
            }
            lastPlayerTargetable = isPlayerTargetable;

            targetCheckTimer = 25;
        }
    }

    protected virtual void OnPlayerBecameTargetable()
    {

    }

    protected Transform GetTargetTrfm()
    {
        if (targetPlayer.targetTrfm)
        {
            return targetPlayer.targetTrfm;
        }
        return targetPlayer.trfm;
    }

    protected void FaceTargetPlayer()
    {
        transform.forward = PlayerController.self.targetTrfm.position - transform.position;
    }
}
