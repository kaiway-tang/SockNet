using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] ushort damage;
    [SerializeField] byte syncMethod;
    [SerializeField] ushort ownerID, teamID;
    public Collider col;
    [SerializeField] uint eventID;
    public Transform trfm;

    public void Init(ushort pOwnerID, ushort pTeamID)
    {
        ownerID = pOwnerID;
        teamID = pTeamID;
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
            if (target.ValidTarget(ownerID, teamID))
            {
                target.TakeDamage(damage, ownerID, teamID, syncMethod, eventID);
            }
        }
    }
}
