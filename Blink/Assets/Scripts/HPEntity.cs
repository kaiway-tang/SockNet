using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public ushort HP;
    public ushort objID;

    public ParticleSystem damageFX;
    public GameObject deathFX;
    public delegate void DamageDelegate(ushort amount, ushort sourceID);
    public event DamageDelegate OnDamage;

    public bool useDefaultBehavior = true;

    [SerializeField] GameObject rootObj;
    ushort lastHPUpdate;

    protected void Start()
    {
        HPBuffer = new byte[5];
        NetworkManager.SetBufferUShort(HPBuffer, objID);
        HPBuffer[2] = 223;
    }

    public void TakeDamage(ushort amount, ushort sourceID, bool propagateUpdate = false)
    {
        if (propagateUpdate)
        {
            SyncHP();
        }
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

    byte[] HPBuffer;
    public void SyncHP()
    {
        NetworkManager.SetBufferUShort(HPBuffer, HP, 3);
        NetworkManager.Send(HPBuffer);
    }

    ushort hpUpdate;
    public void ResolveHP(byte[] buffer)
    {
        hpUpdate = NetworkManager.GetBufferUShort(buffer, 3);
        if (hpUpdate < HP)
        {
            TakeDamage((ushort)(HP - hpUpdate), 0);
        }
    }
}
