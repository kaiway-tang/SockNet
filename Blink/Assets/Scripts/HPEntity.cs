using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public int HP;
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
        OnDamage?.Invoke(amount, sourceID);

        if (useDefaultBehavior)
        {
            HP -= amount;
            if (HP < 0.01)
            {
                HP = 0;
                if (propagateUpdate) { SyncHP(); }
                End();
            }
            else
            {
                damageFX.Play();
            }
        }

        if (propagateUpdate) { SyncHP(); }
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
        NetworkManager.SetBufferUShort(HPBuffer, (ushort)HP, 3);
        Debug.Log("HP sent: " + HPBuffer[0] + " " + HPBuffer[1] + " " + HPBuffer[2] + " " + HPBuffer[3] + " " + HPBuffer[4]);
        NetworkManager.Send(HPBuffer);
    }

    ushort hpUpdate;
    public void ResolveHP(byte[] buffer)
    {
        Debug.Log("HP recv: " + buffer[0] + " " + buffer[1] + " " + buffer[2] + " " + buffer[3] + " " + buffer[4]);
        hpUpdate = NetworkManager.GetBufferUShort(buffer, 3);
        if (hpUpdate < HP)
        {
            TakeDamage((ushort)(HP - hpUpdate), 0);
        } else if (hpUpdate > HP)
        {
            HP = hpUpdate;
        }
    }
}
