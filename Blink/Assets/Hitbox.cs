using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] ushort damage;
    [SerializeField] byte syncMethod;
    [SerializeField] ushort ownerID;
    [SerializeField] Collider col;
    [SerializeField] uint eventID;
    public Transform trfm;

    public void Init(ushort pOwnerID, bool isLocal = false)
    {
        ownerID = pOwnerID;
        trfm = transform;
    }

    public void Activate(uint pEventID = 0)
    {
        eventID = pEventID;
        col.enabled = true;        
        //gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        col.enabled = false;
        //gameObject.SetActive(false);
    }

    HPEntity target;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 6)
        {
            target = other.GetComponent<HPEntity>();
            if (target.objID != ownerID || ownerID == 0)
            {
                target.TakeDamage(damage, ownerID, syncMethod, eventID);
            }
        }
    }
}
