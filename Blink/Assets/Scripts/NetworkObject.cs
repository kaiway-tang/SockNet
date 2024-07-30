using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    bool connected;
    public ushort objID;
    protected void Start()
    {
        
    }

    public bool IsConnected()
    {
        return connected;
    }

    public virtual void AssignObjID(ushort ID)
    {
        objID = ID;
        NetworkManager.networkObjects.Add(ID, GetComponent<NetworkObject>());        
        connected = true;        
    }

    public virtual void NetworkUpdate(byte[] buffer)
    {

    }

    public virtual void OnDcd()
    {
        Destroy(gameObject);
    }
}
