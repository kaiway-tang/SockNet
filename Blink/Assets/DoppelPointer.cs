using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoppelPointer : MonoBehaviour
{
    public Transform doppel;
    [SerializeField] Transform number;
    bool doppelDead;
    void FixedUpdate()
    {
        if (doppel)
        {
            transform.forward = doppel.position - transform.position;
            number.localEulerAngles = Vector3.zero;
        }
        else if (!doppelDead)
        {
            doppelDead = true;
            transform.forward = -PlayerController.self.trfm.forward;
        }  
    }

    private void OnEnable()
    {
        FixedUpdate();
    }
}
