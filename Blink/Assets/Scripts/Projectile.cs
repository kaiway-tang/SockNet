using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected Transform trfm;
    public ushort ownerID;
    [SerializeField] protected ushort damage;

    [SerializeField] protected byte syncMethod;
    [SerializeField] protected uint eventID;
    public virtual void Init(ushort pOwnerID, float timeDelta, byte pSyncMethod = HPEntity.DONT_SYNC, uint pEventID = 0)
    {
        Invoke("End", 5);
        trfm = transform;
        ownerID = pOwnerID;
        syncMethod = pSyncMethod;
        eventID = pEventID;
    }    

    protected virtual void End() { }
}
