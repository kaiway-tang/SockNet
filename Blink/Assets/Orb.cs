using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] MeshRenderer mesh;
    [SerializeField] TDMManager tdmManager;
    bool ready;
    int timer;
    void Start()
    {
        timer = 100;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (timer > 0)
        {
            if (timer == 2000)
            {
                tdmManager.RevealOpponents(false);
            }
            timer--;
        }
        else if (!ready)
        {
            ready = true;
            mesh.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ready && other.gameObject.layer == 0)
        {
            mesh.enabled = false;
            ready = false;
            if (Vector3.Distance(PlayerController.self.trfm.position, transform.position) < 2)
            {
                TDMManager.HealLocalTeam(50);
                tdmManager.RevealOpponents(true);
            }            
            timer = 3000;
        }
    }
}
