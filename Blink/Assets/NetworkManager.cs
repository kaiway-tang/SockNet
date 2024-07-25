using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    static NetworkManager self;
    [SerializeField] TextMeshProUGUI tmp;

    #region BUFFER_TRANSMISSION

    const ushort HALF_SHORT = 32768;

    private void Awake()
    {
        self = GetComponent<NetworkManager>();
    }

    void InitializeBufferSizes()
    {
        playerInputBuffer = new byte[10];
    }
  
    static ushort temp;

    // X - Y - Z - YRot - inputs
    // 2'b - 2'b - 2'b - 2'b - 1'b
    static byte[] playerInputBuffer;
    const int PLAYER_INPUT = 1;

    public static void UpdatePlayerInput(Vector3 pos, bool fwd, bool back, bool left, bool right)
    {
        SetBufferUShort(playerInputBuffer, PlayerController.playerObjID);

        SetBufferCoords(playerInputBuffer, pos);

        SetBufferUShort(playerInputBuffer, PlayerController.self.PlayerObj);

        temp = fwd ? 8 : 0;
        temp += back ? 4 : 0;
        temp += left ? 2 : 0;
        temp += right ? 1 : 0;

        Append(temp, 2);

        //self.tmp.text = packet.ToString();
        //SendLong(packet);
    }

    static void SetBufferCoords(byte[] buffer, Vector3 pos, int startIndex = 2)
    {
        SetBufferUShort(buffer, EncodeCoord(pos.x), 2);
        SetBufferUShort(buffer, EncodeCoord(pos.y), 4);
        SetBufferUShort(buffer, EncodeCoord(pos.z), 6);
    }

    static ushort EncodeCoord(float coord)
    {
        temp = (ushort)(Mathf.RoundToInt(coord * 100) + HALF_SHORT);
        if (temp > 65535 || temp < 0) { temp = HALF_SHORT; }
        return temp;
    }

    static void Append(int segment, int segLength)
    {
        //packet = Mathf.RoundToInt(Mathf.Pow(10, segLength)) * packet;
        //packet += segment;
    }

    static void SetBufferUShort(byte[] buffer, ushort val, int startIndex = 0)
    {
        buffer[startIndex] = (byte)(val >> 8);
        buffer[startIndex + 1] = (byte)(val & 0xFF);
    }

    #endregion

    #region NETWORK_OBJECTS
    static Dictionary<ushort, NetworkObject> networkObjects = new Dictionary<ushort, NetworkObject>();

    void ProcessUpdate(byte[] buffer)
    {
        networkObjects[GetBufferObjID(buffer)].NetworkUpdate(buffer);
    }

    public static ushort GetNewObjID(NetworkObject networkObject, bool permanent = false)
    {
        ushort newID = 0;
        if (permanent)
        {
            newID = 0;
        }
        else
        {
            newID = 1024;
        }
        networkObjects.Add(newID, networkObject);
        return newID;
    }

    static ushort GetBufferObjID(byte[] buffer)
    {
        return (ushort)((buffer[0] << 8) | buffer[1]);
    }
    #endregion

    #region JS_PLUGIN

#if UNITY_WEBGL || !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern void Connect(string url);

    [DllImport("__Internal")]
    private static extern void Send(int high, int low);    

    void Start()
    {
        Connect("ws://192.168.1.212:3300");
    }

    private void Update()
    {

    }

    public static void SendLong(long msg)
    {
        Debug.Log("send long: " + msg);
        Send((int)(msg >> 32), (int)(msg & 0xFFFFFFFF));
    }

    public void JSM(byte[] buffer)
    {
        tmp.text = buffer.ToString();
        ProcessUpdate(buffer);
    }
#endif
}

#endregion