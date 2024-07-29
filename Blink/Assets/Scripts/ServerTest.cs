using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

class ServerTest : NetworkObject
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            byte[] buffer = { 0, 0, 4, 0, 0, 0, 0};
            NetworkManager.SetBufferUShort(buffer, 1, 3);
            NetworkManager.Send(buffer);
        }
    }
}
