using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected Transform trfm;
    public ushort ownerID;
    [SerializeField] protected ushort damage;

    protected bool isLocal;
    public virtual void Init(ushort pOwnerID, float timeDelta, bool pIsLocal)
    {
        Invoke("End", 5);
        trfm = transform;
        ownerID = pOwnerID;
        isLocal = pIsLocal;
    }    

    protected virtual void End() { }
}
