using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    public ushort objID;
    protected void Start()
    {
        objID = NetworkManager.GetNewObjID(GetComponent<NetworkObject>());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void NetworkUpdate(byte[] buffer)
    {

    }
}
