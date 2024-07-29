using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public int HP;
    public ushort objID;

    public ParticleSystem damageFX;
    public delegate void DamageDelegate(int amount, ushort sourceID);
    public event DamageDelegate OnDamage;

    public bool useDefaultBehavior = true;

    public void TakeDamage(int amount, ushort sourceID)
    {
        OnDamage?.Invoke(amount, sourceID);
        if (useDefaultBehavior)
        {
            damageFX.Play();
            HP -= amount;
        }
    }
}
