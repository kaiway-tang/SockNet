using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public int HP;
    public ushort objID;

    public ParticleSystem damageFX;
    public GameObject deathFX;
    public delegate void DamageDelegate(int amount, ushort sourceID);
    public event DamageDelegate OnDamage;

    public bool useDefaultBehavior = true;

    [SerializeField] GameObject rootObj;

    public void TakeDamage(int amount, ushort sourceID)
    {
        if (useDefaultBehavior)
        {
            HP -= amount;
            if (HP < 0.01)
            {                
                End();             
            }
            else
            {
                damageFX.Play();
            }                        
        }

        OnDamage?.Invoke(amount, sourceID);
    }

    public void End()
    {
        if (deathFX) { Instantiate(deathFX, transform.position, Quaternion.identity); }

        if (rootObj) { Destroy(rootObj); }
        else { Destroy(gameObject); }
    }
}
