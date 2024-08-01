using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : Enemy
{
    [SerializeField] GameObject shuriken;
    [SerializeField] Transform firePoint;
    [SerializeField] int timer;
    [SerializeField] AudioSource beep;
    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    new void FixedUpdate()
    {
        base.FixedUpdate();

        if (isPlayerTargetable)
        {
            FaceTargetPlayer();
            if (timer > 0)
            {
                timer--;
            }
            else
            {
                Instantiate(shuriken, firePoint.position, firePoint.rotation).GetComponent<Shuriken>().Init(0, 0, 0);
                timer = 50;
            }
        }        
    }

    protected override void OnPlayerBecameTargetable()
    {
        timer = 50;
        beep.Play();
    }
}
