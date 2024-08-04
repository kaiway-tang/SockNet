using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPEntity : MonoBehaviour
{
    public int HP;
    public ushort objID;
    public ushort teamID;
    public const int TEAM_NONE = 0;

    public ParticleSystem damageFX;
    public AudioSource damageSFX;
    public GameObject deathFX;
    public delegate void DamageDelegate(ushort amount, ushort sourceID);
    public event DamageDelegate OnDamage;
    public delegate void HealDelegate(ushort amount);
    public event HealDelegate OnHeal;

    public bool useDefaultBehavior = true;

    [SerializeField] UIHandler uiHandler;

    [SerializeField] bool dontSync;
    [SerializeField] GameObject[] objs;

    const int FUNCTION_ID = 2, DEATH_UPDATE = 222, HP_UPDATE = 223;

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
        if (HP > 100) { HP = 100; }
        if (uiHandler) { uiHandler.SetHP(HP); }

        OnHeal?.Invoke(amount);

        if (syncMethod > 0) { SyncHP(syncMethod); }        
    }

    public const byte DONT_SYNC = 0, SYNC_HP = 1, SYNC_DMG = 2;
    ushort lastDamageAmount;
    public void TakeDamage(ushort amount, ushort sourceID, ushort teamID = 0, byte syncMethod = DONT_SYNC, uint eventID = 0)
    {
        if (!ValidEventID(eventID)) { return; }
        if (amount < 1) { return; }

        lastDamageAmount = amount;

        if (useDefaultBehavior)
        {
            DefaultDamageBehavior(amount, sourceID, teamID, syncMethod, eventID);
        }

        OnDamage?.Invoke(amount, sourceID);

        if (syncMethod > 0) { SyncHP(syncMethod); }
    }

    public void DefaultDamageBehavior(ushort amount, ushort sourceID, ushort teamID = 0, byte syncMethod = DONT_SYNC, uint eventID = 0)
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
            if (uiHandler) { uiHandler.SetHP(HP); }
        }
    }

    public bool ValidTarget(ushort sourceID, ushort pTeamID)
    {
        //note: wrong

        if (sourceID != 0 && sourceID == objID) { return false; }
        if (pTeamID != 0 && pTeamID == teamID) { return false; }
        return true;
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

        CacheEventID(ID);
        return true;
    }

    void CacheEventID(uint ID)
    {
        lastEventID = ID;
        lastEventIDs[attackIDPtr] = ID;
        attackIDPtr = (attackIDPtr + 1) % lastEventIDs.Length;
    }

    public void End()
    {
        if (deathFX) { Instantiate(deathFX, transform.position, Quaternion.identity); }

        if (objs.Length > 0) {
            for (int i = 0; i < objs.Length; i++)
            {
                Destroy(objs[i]);
            }
        }
        else { Destroy(gameObject); }
    }

    byte[] HPBuffer;
    public void SyncHP(byte syncMethod = 1, uint eventIDOverride = 0)
    {
        if (eventIDOverride > 0) { CacheEventID(eventIDOverride); }
        if (dontSync) { return; }
        HPBuffer[FUNCTION_ID] = HP_UPDATE;
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
            TakeDamage(hpUpdate, 0, TEAM_NONE, DONT_SYNC, eventIDUpdate);
        }
        else
        {
            if (hpUpdate < HP)
            {
                TakeDamage((ushort)(HP - hpUpdate), 0, TEAM_NONE, DONT_SYNC, eventIDUpdate);
            }
            else if (hpUpdate > HP)
            {
                Heal((ushort)(hpUpdate - HP), DONT_SYNC, eventIDUpdate);
            }
        }
    }
}
