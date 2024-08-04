using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected Transform trfm;
    public ushort ownerID, teamID;
    [SerializeField] protected ushort damage;

    [SerializeField] protected byte syncMethod;
    [SerializeField] protected uint eventID;
    [SerializeField] bool isMarker;
    public virtual void Init(ushort pOwnerID, ushort pTeamID, float timeDelta, byte pSyncMethod = HPEntity.DONT_SYNC, uint pEventID = 0)
    {
        Invoke("End", 5);
        trfm = transform;
        ownerID = pOwnerID;
        teamID = pTeamID;
        if (isMarker) { syncMethod = HPEntity.DONT_SYNC; }
        else { syncMethod = pSyncMethod; }

        if (syncMethod == HPEntity.DONT_SYNC || pEventID == 0) { eventID = Tools.RandomEventID(); }
        else { eventID = pEventID; }
    }    

    protected virtual void End() { }
}
