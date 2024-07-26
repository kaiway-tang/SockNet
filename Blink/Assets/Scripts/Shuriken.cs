using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shuriken : Projectile
{
    [SerializeField] ParticleSystem trail;
    [SerializeField] float speed;

    Transform trfm;
    new void Start()
    {
        Invoke("End", 5);
        trfm = transform;
        //base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        trfm.position += trfm.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 6 || other.gameObject.layer == 8)
        {
            if (other.gameObject.layer == 6 && other.GetComponent<HPEntity>().objID == ownerID) { return; }
            Debug.Log("hit: " + other.gameObject.name);
            End();
        }
    }

    void End()
    {
        trail.Stop();
        trail.transform.parent = null;
        Destroy(trail.gameObject, 3);
        Destroy(gameObject);
    }
}
