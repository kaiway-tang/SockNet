using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine.SceneManagement;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager self;
    [SerializeField] TextMeshProUGUI tmp;

    [SerializeField] GameObject cubeFlashInd;

    #region NETWORK_OBJECTS

    public GameObject[] networkPrefabs;

    public static Dictionary<ushort, NetworkObject> networkObjects = new Dictionary<ushort, NetworkObject>();
    public static Dictionary<ushort, PlayerController> players = new Dictionary<ushort, PlayerController>();

    public static float time;

    public bool isWebGL = false;

    void ProcessUpdate(byte[] buffer)
    {
        ushort objID = GetBufferObjID(buffer);
        if (networkObjects.ContainsKey(objID)){
            networkObjects[objID].NetworkUpdate(buffer);
        }
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
        output += DecodeValue(GetBufferUShort(buffer, 11));
        output += ", Inputs: ";
        output += buffer[15];

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

    string url = "ws://" + ENV.SPVCB8L_IP + ":" + ENV.DEFAULT_PORT;
    private void Start()
    {        
        if (connected) { return; }

        if (isWebGL)
        {
            WebGL_Start();
        }
        else
        {
            Exe_Start();
        }

        nextSync = 1;
    }

    float reconnectCD;
    int lastSecond;
    private void Update()
    {
        //tmp.text = System.DateTime.Now.ToString() + System.DateTime.Now.Millisecond.ToString();

        if (Input.GetKeyDown(KeyCode.R)) { SceneManager.LoadScene("TDM"); }
        if (reconnectCD > 0) { reconnectCD--; }

        if (isWebGL) { WebGL_Update(); }
        SyncTime_Update();

        time += Time.deltaTime;
        if (timeSynced && time > nextSync && time - nextSync < 50)
        {
            nextSync = (nextSync + 10) % 60;
            offsets[0] = offsets[offsets.Length / 2];
            offsets_index = 1;
            SyncTime();
        }
        if (time > 60) { time -= 60; }

        if (lastSecond != Mathf.RoundToInt(time))
        {
            cubeFlashInd.SetActive(Mathf.RoundToInt(time) % 2 == 0);
            lastSecond = Mathf.RoundToInt(time);
        }
    }

    private void FixedUpdate()
    {

    }

    void InitializeBufferSizes()
    {
        
    }
  
    static int temp;

    public static void SetBufferCoords(byte[] buffer, Vector3 pos, int startIndex = 5)
    {
        SetBufferUShort(buffer, EncodeValue(pos.x), startIndex);
        SetBufferUShort(buffer, EncodeValue(pos.y), startIndex + 2);
        SetBufferUShort(buffer, EncodeValue(pos.z), startIndex + 4);
    }

    public static ushort EncodePosValue(float val, int leftShift = 100)
    {
        temp = Mathf.RoundToInt(val * leftShift);
        if (temp > 65535 || temp < 0) { temp = 0; }
        return (ushort)temp;
    }

    public static ushort EncodeValue(float val, int leftShift = 100)
    {
        temp = Mathf.RoundToInt(val * leftShift) + HALF_SHORT;
        if (temp > 65535 || temp < 0) { temp = HALF_SHORT; }
        return (ushort)temp;
    }

    public static void SetBufferUShort(byte[] buffer, ushort val, int startIndex = 0)
    {        
        buffer[startIndex] = (byte)(val >> 8);
        buffer[startIndex + 1] = (byte)(val & 0xFF);
    }
    public static void SetBufferUInt(byte[] buffer, uint val, int startIndex = 0)
    {
        buffer[startIndex + 3] = (byte)(val & 0xFF);
        val = val >> 8;
        buffer[startIndex + 2] = (byte)(val & 0xFF);
        val = val >> 8;
        buffer[startIndex + 1] = (byte)(val & 0xFF);
        val = val >> 8;
        buffer[startIndex] = (byte)(val & 0xFF);
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

    public static float DecodePosValue(ushort val, int leftShift = 100)
    {
        return (float)val / leftShift;
    }

    public static float DecodeValue(ushort val, int leftShift = 100)
    {
        return (float)(val - HALF_SHORT) / leftShift;
    }

    public static ushort GetBufferUShort(byte[] buffer, int startIndex = 0)
    {
        return (ushort)(buffer[startIndex] << 8 | buffer[startIndex + 1]);
    }

    public static uint GetBufferUInt(byte[] buffer, int startIndex = 0)
    {
        return (uint)(buffer[startIndex] << 24 | buffer[startIndex + 1] << 16 | buffer[startIndex + 2] << 8 | buffer[startIndex + 3]);
    }

    static float timeDelta = 0;
    public static float GetBufferDelta(byte[] buffer, int startIndex = 0)
    {
        timeDelta = time - GetBufferUShort(buffer, startIndex) / 1000f;
        if (timeDelta < -0.001f)
        {
            timeDelta += 60;
        }
        return timeDelta;
    }

    const int INIT_MSG = 0, NET_OBJ_INIT = 1, SYNC_TIME_MSG = 3, CLIENT_DCD = 4;
    void ResolveServerMessage(byte[] buffer)
    {
        if (buffer[2] == INIT_MSG)
        {
            isHost = buffer[3] == 1;
        }
        else if (buffer[2] == NET_OBJ_INIT)
        {
            ResolveNetObjInit(buffer);
        }
        else if (buffer[2] == SYNC_TIME_MSG)
        {
            ResolveTime(buffer);
        }
        else if (buffer[2] == CLIENT_DCD)
        {
            ResolveDcd(buffer);
        }
    }

    #endregion

    #region SOCKETS

    private static ClientWebSocket webSocket = null;
    private static CancellationTokenSource cancellation = new CancellationTokenSource();

    async void Exe_Start()
    {
        await Connect(new Uri(url));
    }

    bool isConnecting;
    private async Task Connect(Uri uri)
    {
        if (isConnecting) { return; }
        isConnecting = true;
        webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(uri, cancellation.Token);
        //Debug.Log("WebSocket connection established!");

        // Start listening for incoming messages
        connected = true;
        isConnecting = false;
        byte[] initialMessage = { 0, 0, 0 };
        await ExeSend(initialMessage);
        SyncTime();
        for (int i = 0; i < initializationQue.Count; i++)
        {
            Send(initializationQue[i]);
        }
        await Receive();
    }

    byte[] _buffer = new byte[20];
    private async Task Receive()
    {
        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = null;
            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellation.Token);
            }
            catch (Exception e)
            {
                Debug.LogError("WebSocket receive error: " + e.Message);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Debug.Log("WebSocket connection closed.");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellation.Token);
            }
            else
            {
                //var receivedData = ConvertArraySegmentToByteArray(new ArraySegment<byte>(buffer, 0, result.Count));

                if (_buffer[1] == 0 && _buffer[0] == 0)
                {
                    ResolveServerMessage(_buffer);
                }
                else
                {
                    ProcessUpdate(_buffer);
                }
            }
        }
    }

    public static async Task ExeSend(byte[] buffer)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            //var encodedMessage = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
            await webSocket.SendAsync(arraySegment, WebSocketMessageType.Binary, true, cancellation.Token);
        }
        else
        {
            Debug.LogWarning("WebSocket is not connected: " + webSocket.State);
            //reconnect?
        }
    }

    private async Task HandleDisconnection()
    {
        connected = false;
        if (!cancellation.Token.IsCancellationRequested)
        {
            cancellation.Cancel();
        }

        cancellation = new CancellationTokenSource();
        if (reconnectCD <= 0)
        {
            reconnectCD = 5;
            Debug.Log("Reconnecting...");
            await Connect(new Uri(url));
        }
    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application ending", CancellationToken.None);
        }
    }

    #endregion

    #region JS_PLUGIN

    [DllImport("__Internal")]
    private static extern void Connect(string url);

    [DllImport("__Internal")]
    private static extern void JSSend(byte[] buffer, int length);    

    static bool connected;
    static bool timeSynced = false;

    public static async void Send(byte[] buffer)
    {
        if (connected)
        {
            if (self.isWebGL)
            {
                JSSend(buffer, buffer.Length);
            }
            else
            {
                await ExeSend(buffer);
            }            
        }
    }

//#if UNITY_WEBGL && !UNITY_EDITOR

    void WebGL_Start()
    {
        Connect(url);
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

    public delegate void OnJoinDelegate(ushort objID);
    public event OnJoinDelegate OnJoin;

    public delegate void JoinTeamDelegate(ushort objID, ushort teamID);
    public event JoinTeamDelegate JoinTeam;
    static void ResolveNetObjInit(byte[] buffer)
    {
        ushort param_ID = GetBufferUShort(buffer, 3);        
        if (param_ID < 1024)
        {
            Debug.Log("Assigned ID: " + GetBufferUShort(buffer, 5));
            initializationLinker[param_ID].AssignObjID(GetBufferUShort(buffer, 5));
            initializationLinker.Remove(param_ID);
        }
        else
        {
            if (param_ID > self.networkPrefabs.Length + 1024) { Debug.Log("Invalid prefab ID: " + param_ID); }
            else
            {
                Debug.Log("Init NetObj ID: " + (param_ID - 1024));
                newNetObj = Instantiate(self.networkPrefabs[param_ID - 1024]).GetComponent<NetworkObject>();
                newNetObj.AssignObjID(GetBufferUShort(buffer, 5));

                if (param_ID - 1024 == 0)
                {
                    players.Add(newNetObj.objID, newNetObj.GetComponent<PlayerController>());
                    self.OnJoin?.Invoke(newNetObj.objID);
                }
            }            
        }
    }

    static void ResolveDcd(byte[] buffer)
    {        
        ushort id = GetBufferUShort(buffer, 3);        
        if (networkObjects.ContainsKey(id))
        {
            Debug.Log("Disconnecting client ID #" + id);
            networkObjects[id].OnDcd();
            networkObjects.Remove(id);
        }
        else
        {
            Debug.Log("Cannot disconnect client ID #" + id);
        }
    }

    public void JSM(string msg)
    {
        byte[] buffer = Convert.FromBase64String(msg);
        //self.tmp.text = FormatString(buffer);

        if (buffer[1] == 0 && buffer[0] == 0)
        {
            ResolveServerMessage(buffer);
        }
        else { ProcessUpdate(buffer); }
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

        SyncTime();
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
        server_time = GetBufferUShort(buffer, 3) / 1000f + latency;
        if (overflow_correction)
        {
            server_time = (server_time + 30) % 60;
        }
        else
        {
            if (server_time > 52 && offsets_index == 0)
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
            //Debug.Log("final difference: " + offsets[offsets.Length / 2]);
            time += offsets[offsets.Length/2];
            if (overflow_correction)
            {
                time -= 30;
                if (time < 0) { time += 60; }
            }

            offsets_index = 0;
            overflow_correction = false;

            if (!timeSynced)
            {
                timeSynced = true;
                nextSync = (time - time % 5 + 6) % 60;
            }            
        }
        else
        {
            pingTimer = 0.25f;
            //PingTime();
        }
    }

    void SyncTime()
    {
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