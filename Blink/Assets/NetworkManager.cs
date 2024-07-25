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
        InitializeBufferSizes();
    }

    void InitializeBufferSizes()
    {
        playerInputBuffer = new byte[11];
    }
  
    static int temp;

    // X - Y - Z - YRot - inputs
    // 2'b - 2'b - 2'b - 2'b - 1'b
    static byte[] playerInputBuffer;
    const int PLAYER_INPUT = 1;

    public static void UpdatePlayerInput(Vector3 pos, bool fwd, bool back, bool left, bool right)
    {
        SetBufferUShort(playerInputBuffer, PlayerController.playerObjID);

        SetBufferCoords(playerInputBuffer, pos);

        SetBufferUShort(playerInputBuffer, EncodeValue(PlayerController.self.PlayerObj.eulerAngles.y), 8);

        temp = fwd ? 8 : 0;
        temp += back ? 4 : 0;
        temp += left ? 2 : 0;
        temp += right ? 1 : 0;

        playerInputBuffer[10] = (byte)temp;

        //Debug.Log(FormatString(playerInputBuffer));
        //self.tmp.text = FormatString(playerInputBuffer);
        Send(playerInputBuffer, 11);
    }

    static void SetBufferCoords(byte[] buffer, Vector3 pos, int startIndex = 2)
    {
        SetBufferUShort(buffer, EncodeValue(pos.x), 2);
        SetBufferUShort(buffer, EncodeValue(pos.y), 4);
        SetBufferUShort(buffer, EncodeValue(pos.z), 6);
    }

    static ushort EncodeValue(float val, int leftShift = 100)
    {
        temp = (Mathf.RoundToInt(val * leftShift) + HALF_SHORT);
        if (temp > 65535 || temp < 0) { temp = HALF_SHORT; }
        return (ushort)temp;
    }

    static void SetBufferUShort(byte[] buffer, ushort val, int startIndex = 0)
    {
        buffer[startIndex] = (byte)(val >> 8);
        buffer[startIndex + 1] = (byte)(val & 0xFF);
    }



    static Vector3 tempVect;
    public static Vector3 GetBufferCoords(byte[] buffer, int startIndex = 2)
    {
        tempVect.x = DecodeValue(GetBufferUShort(buffer, startIndex));
        tempVect.y = DecodeValue(GetBufferUShort(buffer, startIndex + 2));
        tempVect.z = DecodeValue(GetBufferUShort(buffer, startIndex + 4));

        return tempVect;
    }

    public static float DecodeValue(ushort val, int leftShift = 100)
    {
        return (float)(val - HALF_SHORT) / leftShift;
    }

    public static ushort GetBufferUShort(byte[] buffer, int startIndex = 0)
    {
        return (ushort)(buffer[startIndex] << 8 | buffer[startIndex + 1]);
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

    public static ushort GetBufferObjID(byte[] buffer)
    {
        return (ushort)((buffer[0] << 8) | buffer[1]);
    }

    public static string FormatString(byte[] buffer)
    {
        string output = "ObjID: ";
        output += GetBufferObjID(buffer);
        output += ", Coords: ";
        output += GetBufferCoords(buffer);
        output += ", YRot: ";
        output += DecodeValue(GetBufferUShort(buffer, 8));
        output += ", Inputs: ";
        output += buffer[10];

        return output;
    }
    #endregion

    #region JS_PLUGIN

#if UNITY_WEBGL || !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern void Connect(string url);

    [DllImport("__Internal")]
    private static extern void Send(byte[] buffer, int length);    

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
        //Send((int)(msg >> 32), (int)(msg & 0xFFFFFFFF));
    }

    public void JSM(string msg)
    {
        byte[] buffer = System.Convert.FromBase64String(msg);
        self.tmp.text = FormatString(buffer);
        ProcessUpdate(buffer);
    }
#endif
}

#endregion