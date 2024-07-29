using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected Transform trfm;
    public ushort ownerID;
    [SerializeField] protected int damage;
    public virtual void Init(ushort pOwnerID, float timeDelta)
    {
        Invoke("End", 5);
        trfm = transform;
        ownerID = pOwnerID;
    }    

    protected virtual void End() { }
}
