using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Text;

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

    public static float time;

    public bool isWebGL = false;

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

#if UNITY_WEBGL && !UNITY_EDITOR
        isWebGL = true;
#endif
    }

    private void Start()
    {        
        if (isWebGL)
        {
            WebGL_Start("ws://" + ENV.DEFAULT_IP + ":" + ENV.DEFAULT_PORT);
        }
        else
        {
            ExeConnect(ENV.DEFAULT_IP, ENV.DEFAULT_PORT_INT);
        }

        nextSync = 1;
    }

    int lastSecond;
    private void Update()
    {
        //tmp.text = System.DateTime.Now.ToString() + System.DateTime.Now.Millisecond.ToString();


        if (isWebGL) { WebGL_Update(); }
        SyncTime_Update();

        time += Time.deltaTime;
        if (connected && time > nextSync)
        {
            nextSync = (nextSync + 5) % 60;
            SyncTime();
        }
        if (time > 60) { time -= 60; }

        if (lastSecond != Mathf.RoundToInt(time))
        {
            cubeFlashInd.SetActive(Mathf.RoundToInt(time) % 2 == 0);
            lastSecond = Mathf.RoundToInt(time);
        }
    }

    void InitializeBufferSizes()
    {
        playerInputBuffer = new byte[16];
    }
  
    static int temp;

    // X - Y - Z - YRot - inputs
    // 2'b - 2'b - 2'b - 2'b - 1'b
    static byte[] playerInputBuffer;
    const int PLAYER_INPUT = 1;

    public static void UpdatePlayerInput(Vector3 pos, bool fwd, bool back, bool left, bool right)
    {
        SetBufferUShort(playerInputBuffer, PlayerController.playerObjID);

        SetBufferTime(playerInputBuffer, 3);

        SetBufferCoords(playerInputBuffer, pos);

        SetBufferUShort(playerInputBuffer, EncodeValue(PlayerController.self.PlayerObj.eulerAngles.y), 11);
        SetBufferUShort(playerInputBuffer, EncodeValue(PlayerController.self.PlayerObj.eulerAngles.x), 13);

        temp = fwd ? 8 : 0;
        temp += back ? 4 : 0;
        temp += left ? 2 : 0;
        temp += right ? 1 : 0;

        playerInputBuffer[15] = (byte)temp;

        //Debug.Log(FormatString(playerInputBuffer));
        //self.tmp.text = FormatString(playerInputBuffer);

        Send(playerInputBuffer);
    }

    static void SetBufferCoords(byte[] buffer, Vector3 pos, int startIndex = 5)
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

    public static void SetBufferTime(byte[] buffer, int startIndex = 0)
    {
        SetBufferUShort(buffer, (ushort)Mathf.RoundToInt(time * 1000), startIndex);
    }

    static Vector3 tempVect;
    public static Vector3 GetBufferCoords(byte[] buffer, int startIndex = 5)
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

    public static float GetBufferDelay(byte[] buffer, int startIndex = 0)
    {
        return Mathf.RoundToInt(time * 1000) - GetBufferUShort(buffer, startIndex);
    }

    #endregion

    #region SOCKETS

    private TcpClient tcpClient;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];

    private void ExeConnect(string address, int port)
    {
        try
        {
            tcpClient = new TcpClient(address, port);
            stream = tcpClient.GetStream();
            Debug.Log("Connected to server.");
            connected = true;

            byte[] initial_message = { 0, 0, 0 };
            ExeSend(initial_message);

            // Start listening for incoming data
            BeginRead();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting to server: {e.Message}");
        }
    }

    private void BeginRead()
    {
        if (stream != null)
        {
            stream.BeginRead(buffer, 0, buffer.Length, OnDataReceived, null);
        }
    }

    private void OnDataReceived(IAsyncResult ar)
    {
        try
        {
            int bytesRead = stream.EndRead(ar);
            if (bytesRead > 0)
            {
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"Received data: {receivedData}");
                // Continue reading data
                BeginRead();
            }
            else
            {
                Debug.Log("Connection closed by server.");
                CloseConnection();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving data: {e.Message}");
            CloseConnection();
        }
    }

    public void ExeSend(byte[] buffer)
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }

    private void CloseConnection()
    {
        if (stream != null)
        {
            stream.Close();
        }
        if (tcpClient != null)
        {
            tcpClient.Close();
        }
    }

    private void OnDestroy()
    {
        CloseConnection();
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

    void WebGL_Start(string address)
    {
        Connect(address);
    }

    private void WebGL_Update()
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
        ushort param_ID = GetBufferUShort(buffer, 3);
        Debug.Log("NetObjInit param: " + param_ID);
        if (param_ID < 1024)
        {
            initializationLinker[param_ID].AssignObjID(GetBufferUShort(buffer, 5));
            initializationLinker.Remove(param_ID);
        }
        else
        {
            newNetObj = Instantiate(self.networkPrefabs[param_ID - 1024]).GetComponent<NetworkObject>();
            newNetObj.AssignObjID(GetBufferUShort(buffer, 5));
        }
    }

    public void JSM(string msg)
    {
        byte[] buffer = System.Convert.FromBase64String(msg);
        //self.tmp.text = FormatString(buffer);

        if (buffer[1] == 0 && buffer[0] == 0)
        {
            Debug.Log(buffer[3] == 0);

            if (buffer[2] == 0)
            {
                isHost = buffer[3] == 1;
            }
            else if (buffer[2] == 1)
            {
                ResolveNetObjInit(buffer);
            }
            else if (buffer[2] == 3)
            {
                ResolveTime(buffer);
            }
        }
        else
        {
            ProcessUpdate(buffer);
        }        
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

        time = 0.5f;
    }
    //#endif

    #endregion

    #region SYNC_TIME

    float latency;
    float[] offsets = new float[7];
    int offsets_index;
    float send_time, server_time;
    bool overflow_correction = false;

    float nextSync = 1;
    float pingTimer;

    void SyncTime_Update()
    {
        if (pingTimer > 0)
        {
            pingTimer -= Time.deltaTime;
            if (pingTimer <= 0)
            {
                PingTime();
            }
        }
    }

    void ResolveTime(byte[] buffer)
    {
        latency = (time - send_time) / 2;
        Debug.Log("ping: " + latency);
        server_time = GetBufferUShort(buffer, 3) / 1000f + latency;
        if (overflow_correction)
        {
            server_time = (server_time + 30) % 60;
        }
        else
        {
            if (server_time > 54 && offsets_index == 0)
            {
                overflow_correction = true;
                PingTime();
                return;
            }
        }
        offsets[offsets_index] = server_time - time;
        offsets_index++;

        if (offsets_index >= offsets.Length)
        {
            Array.Sort(offsets);
            Debug.Log("final difference: " + offsets[offsets.Length / 2]);
            time += offsets[offsets.Length/2];
            if (overflow_correction)
            {
                time -= 30;
                if (time < 0) { time += 60; }
            }

            offsets_index = 0;
            overflow_correction = false;
        }
        else
        {
            pingTimer = 0.25f;
        }
    }

    void SyncTime()
    {
        time = 0;
        PingTime();
    }

    void PingTime()
    {
        byte[] time_buffer = { 0, 0, 3 };
        send_time = time;
        Send(time_buffer);
    }

    #endregion
}