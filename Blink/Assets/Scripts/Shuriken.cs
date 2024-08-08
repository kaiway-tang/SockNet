using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shuriken : Projectile
{
    [SerializeField] ParticleSystem trail;
    [SerializeField] float speed;
    [SerializeField] Transform modelTrfm;
    [SerializeField] Rigidbody rb;
    [SerializeField] AudioSource embed;

    bool init = false;

    public override void Init(ushort ownerID, ushort pTeamID, float timeDelta, byte pSyncMethod = HPEntity.DONT_SYNC, uint pEventID = 0)
    {        
        base.Init(ownerID, pTeamID, timeDelta, pSyncMethod, pEventID);
        trfm.position += trfm.forward * speed * timeDelta;
        rb.velocity = trfm.forward * speed;
        init = true;
    }

    // Update is called once per frame
    void Update()
    {
        //trfm.position += trfm.forward * speed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        modelTrfm.Rotate(Vector3.forward * 15);
    }

    HPEntity target;
    private void OnTriggerEnter(Collider other)
    {
        if (!init) { return; }
        if (other.gameObject.layer == 6)
        {
            target = other.gameObject.GetComponent<HPEntity>();
            if (target.ValidTarget(ownerID, teamID))
            {
                target.TakeDamage(damage, ownerID, teamID, syncMethod, eventID);
            }
            else
            {
                return;
            }
            leaveShuriken = false;
            End();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8)
        {
            leaveShuriken = true;
            End();
        }
    }

    bool leaveShuriken;
    protected override void End()
    {
        embed.Play();
        trail.Stop();
        trail.transform.parent = null;
        Destroy(trail.gameObject, 5);
        if (leaveShuriken)
        {
            modelTrfm.parent = null;
            Destroy(modelTrfm, 60);
        }              
        Destroy(gameObject);
    }
}
