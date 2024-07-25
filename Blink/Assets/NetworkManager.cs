using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    static NetworkManager self;
    [SerializeField] TextMeshProUGUI tmp;

    private void Awake()
    {
        self = GetComponent<NetworkManager>();
    }


    // 0 - 00000 - 00000 - 00000 - 00
    const int PLAYER_INPUT = 1;
    static int temp;

    static long packet;
    void UpdateNetworkObject()
    {

    }

    public static void UpdatePlayerInput(Vector3 pos, bool fwd, bool back, bool left, bool right)
    {
        packet = PLAYER_INPUT;
        
        Append(EncodeCoord(pos.x), 5);
        Append(EncodeCoord(pos.y), 5);
        Append(EncodeCoord(pos.z), 5);

        Debug.Log(packet);

        temp = fwd ? 8 : 0;
        temp += back ? 4 : 0;
        temp += left ? 2 : 0;
        temp += right ? 1 : 0;

        Append(temp, 2);

        //self.tmp.text = packet.ToString();
        SendLong(packet);
    }

    static int EncodeCoord(float coord)
    {
        temp = Mathf.RoundToInt(coord * 100) + 50000;
        if (temp > 99999 || temp < 0) { temp = 50000; }
        return temp;
    }

    static void Append(int segment, int segLength)
    {
        packet = Mathf.RoundToInt(Mathf.Pow(10, segLength)) * packet;
        packet += segment;
    }

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

    public void JSM(string message)
    {
        tmp.text = message;
    }
#endif
}

public class Packet
{

}