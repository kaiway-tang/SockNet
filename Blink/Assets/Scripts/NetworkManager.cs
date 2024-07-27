using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    static NetworkManager self;
    [SerializeField] TextMeshProUGUI tmp;

    [SerializeField] GameObject cubeFlashInd;

    #region NETWORK_OBJECTS

    public GameObject[] networkPrefabs;

    public static Dictionary<ushort, NetworkObject> networkObjects = new Dictionary<ushort, NetworkObject>();
    public static PlayerController[] players;

    void ProcessUpdate(byte[] buffer)
    {
        networkObjects[GetBufferObjID(buffer)].NetworkUpdate(buffer);
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

    #region BUFFER_TRANSMISSION

    const ushort HALF_SHORT = 32768;

    public static bool isHost;

    private void Awake()
    {
        self = GetComponent<NetworkManager>();
        InitializeBufferSizes();
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLStart();
#endif
    }

    int lastSecond;
    private void Update()
    {
        //tmp.text = System.DateTime.Now.ToString() + System.DateTime.Now.Millisecond.ToString();

        #if UNITY_WEBGL && !UNITY_EDITOR
        WebGLUpdate();
        #endif

        return;
        if (lastSecond != System.DateTime.Now.Second)
        {
            cubeFlashInd.SetActive(!cubeFlashInd.activeSelf);
            lastSecond = System.DateTime.Now.Second;
        }
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

        Send(playerInputBuffer);
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

#region JS_PLUGIN

    [DllImport("__Internal")]
    private static extern void Connect(string url);

    [DllImport("__Internal")]
    private static extern void JSSend(byte[] buffer, int length);    

    static bool connected;

    public static bool Send(byte[] buffer)
    {
        if (connected)
        {
            JSSend(buffer, buffer.Length);
            return true;
        }
        return false;
    }

//#if UNITY_WEBGL && !UNITY_EDITOR

    void WebGLStart()
    {
        Connect("ws://" + ENV.SPVCB8L_IP + ":" + ENV.DEFAULT_PORT);
    }

    private void WebGLUpdate()
    {

    }

    static ushort tempID = 0;
    static byte[] syncObjBuffer = { 0, 0, 2, 0, 0, 0, 0};
    static Dictionary<ushort, NetworkObject> initializationLinker = new Dictionary<ushort, NetworkObject>();

    static List<byte[]> initializationQue = new List<byte[]>();

    public static void InitializeNetObj(NetworkObject netObj, ushort prefabID)
    {
        SetBufferUShort(syncObjBuffer, prefabID, 3);
        SetBufferUShort(syncObjBuffer, tempID, 5);
        initializationLinker.Add(tempID, netObj);

        Debug.Log("prefabID: " + prefabID + " qued with tempID: " + tempID);
        tempID++;
        if (tempID >= 1024) { tempID = 0; }

        if (connected) { Send(syncObjBuffer); }
        else { initializationQue.Add(syncObjBuffer); }
    }

    static NetworkObject newNetObj;
    static void ResolveNetObjInit(byte[] buffer)
    {
        Debug.Log("begin resolve");
        ushort param_ID = GetBufferUShort(buffer, 3);
        Debug.Log("handling paramID: " + param_ID);
        if (param_ID < 1024)
        {
            Debug.Log("tempID: " + param_ID + " received ID: " + GetBufferUShort(buffer, 5));
            initializationLinker[param_ID].AssignObjID(GetBufferUShort(buffer, 5));
            initializationLinker.Remove(param_ID);
        }
        else
        {
            Debug.Log("client received sync request: " + param_ID);
            newNetObj = Instantiate(self.networkPrefabs[param_ID - 1024]).GetComponent<NetworkObject>();
            Debug.Log("instantiated: " + self.networkPrefabs[param_ID - 1024]);
            newNetObj.AssignObjID(GetBufferUShort(buffer, 5));
        }
    }

    public void JSM(string msg)
    {
        byte[] buffer = System.Convert.FromBase64String(msg);
        //self.tmp.text = FormatString(buffer);

        if (buffer[1] == 0 && buffer[0] == 0)
        {
            Debug.Log("received special message: " + msg);
            Debug.Log(buffer[3] == 0);
            if (buffer[2] == 0) { isHost = buffer[3] == 1; }
            else if (buffer[2] == 1)
            {
                ResolveNetObjInit(buffer);
            }
        }

        ProcessUpdate(buffer);
    }

    public void ConnectSuccessful()
    {
        connected = true;

        byte[] initial_message = { 0, 0, 0 };
        Send(initial_message);

        for (int i = 0; i < initializationQue.Count; i++)
        {
            Send(initializationQue[i]);
        }
    }
//#endif
}

#endregion