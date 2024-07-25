using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : NetworkObject
{
    public byte[] packet;
    // Start is called before the first frame update
    new void Start()
    {
        packet = new byte[4];
    }

    // Update is called once per frame
    void Update()
    {
        packet[0] = 10;
        //Debug.Log(packet);
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.U))
        {
            packet[0] += 1;
        }
        if (Input.GetKey(KeyCode.O))
        {
            packet[2] += 1;
        }
    }
}
