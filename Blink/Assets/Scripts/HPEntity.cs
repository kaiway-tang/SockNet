using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public int HP;
    public ushort objID;

    public ParticleSystem damageFX;
    public AudioSource damageSFX;
    public GameObject deathFX;
    public delegate void DamageDelegate(ushort amount, ushort sourceID);
    public event DamageDelegate OnDamage;

    public bool useDefaultBehavior = true;

    [SerializeField] bool dontSync;
    [SerializeField] GameObject rootObj;    

    protected void Start()
    {
        HPBuffer = new byte[10];
        NetworkManager.SetBufferUShort(HPBuffer, objID);
        HPBuffer[2] = 223;

        lastEventIDs = new uint[8];
    }

    public void Heal(ushort amount, byte syncMethod = DONT_SYNC, uint eventID = 0)
    {
        if (!ValidEventID(eventID)) { return; }
        HP += amount;
    }

    public const byte DONT_SYNC = 0, SYNC_HP = 1, SYNC_DMG = 2;
    ushort lastDamageAmount;
    public void TakeDamage(ushort amount, ushort sourceID, byte syncMethod = DONT_SYNC, uint eventID = 0)
    {
        if (!ValidEventID(eventID)) { return; }

        lastDamageAmount = amount;

        OnDamage?.Invoke(amount, sourceID);

        if (useDefaultBehavior)
        {
            HP -= amount;
            if (HP < 0.01)
            {
                HP = 0;
                if (syncMethod > 0) { SyncHP(syncMethod); }
                End();
            }
            else
            {
                damageSFX.Play();
                damageFX.Play();
            }
        }

        if (syncMethod > 0) { SyncHP(syncMethod); }
    }

    [SerializeField] int attackIDPtr;
    [SerializeField] uint lastEventID;
    [SerializeField] uint[] lastEventIDs;
    public bool ValidEventID(uint ID)
    {
        if (ID < 1) { return true; }

        for (int i = 0; i < lastEventIDs.Length; i++)
        {
            if (lastEventIDs[i] == ID) { return false; }
        }

        lastEventID = ID;
        lastEventIDs[attackIDPtr] = ID;
        attackIDPtr = (attackIDPtr + 1) % lastEventIDs.Length;
        return true;
    }

    public void End()
    {
        if (deathFX) { Instantiate(deathFX, transform.position, Quaternion.identity); }

        if (rootObj) { Destroy(rootObj); }
        else { Destroy(gameObject); }
    }

    byte[] HPBuffer;
    public void SyncHP(byte syncMethod = 1)
    {
        if (dontSync) { return; }
        HPBuffer[3] = (byte)(syncMethod - 1);
        NetworkManager.SetBufferUInt(HPBuffer, lastEventID, 6);

        if (syncMethod == SYNC_HP)
        {
            if (HP < 0) { HP = 0; NetworkManager.SetBufferUShort(HPBuffer, 0, 4); }
            else { NetworkManager.SetBufferUShort(HPBuffer, (ushort)HP, 4); }
        }
        else if (syncMethod == SYNC_DMG)
        {            
            NetworkManager.SetBufferUShort(HPBuffer, lastDamageAmount, 4);
        }
        NetworkManager.Send(HPBuffer);
    }

    uint eventIDUpdate;
    ushort hpUpdate;
    public void HandleSync(byte[] buffer)
    {
        hpUpdate = NetworkManager.GetBufferUShort(buffer, 4);
        eventIDUpdate = NetworkManager.GetBufferUInt(buffer, 6);

        if (buffer[3] == 1)
        {
            Debug.Log("Took " + hpUpdate + " virtual damage");
            TakeDamage(hpUpdate, 0, DONT_SYNC, eventIDUpdate);
        }
        else
        {
            if (hpUpdate < HP)
            {
                TakeDamage((ushort)(HP - hpUpdate), 0, DONT_SYNC, eventIDUpdate);
            }
            else if (hpUpdate > HP)
            {
                Heal((ushort)(hpUpdate - HP), DONT_SYNC, eventIDUpdate);
            }
        }
    }
}
