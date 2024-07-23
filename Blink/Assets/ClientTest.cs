// WebSocketClient.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class ClientTest : MonoBehaviour
{
    private ClientWebSocket webSocket = null;
    public string uri;
    public Text outputText;

    [SerializeField] MeshRenderer cubeMesh;
    [SerializeField] Material redMat;

    async void Start()
    {
        webSocket = new ClientWebSocket();
        Debug.Log("Attempting: " + uri);
        await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
        Debug.Log("Connected to server");

        // Send a test message
        string message = "Hello, Server!";
        await SendMessageAsync(message);

        // Start receiving messages
        await ReceiveMessages();
    }

    async Task SendMessageAsync(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(messageBytes);
        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        Debug.Log($"Sent message: {message}");
    }

    async Task ReceiveMessages()
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Debug.Log($"Received message: {message}");
            //outputText.text = $"Received: {message}";
            cubeMesh.material = redMat;
        }        
    }

    private void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            webSocket.Dispose();
        }
    }
}
