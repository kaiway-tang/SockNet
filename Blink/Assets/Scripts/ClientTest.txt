using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;
using System.Collections;

public class ClientTest : MonoBehaviour
{
    private WebSocket webSocket;
    public string uri = "ws://47.229.102.209:3300";
    public Text outputText;

    [SerializeField] MeshRenderer cubeMesh;
    [SerializeField] Material redMat;

    async void Start()
    {
        webSocket = new WebSocket(uri);

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to server");
            SendMessage("Hello, Server!");
        };

        webSocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message: {message}");
            // Update UI on the main thread
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                outputText.text = $"Received: {message}";
                cubeMesh.material = redMat;
            });
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError($"WebSocket error: {e}");
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket closed");
        };

        // Connect to the server
        await webSocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket.DispatchMessageQueue();
#endif
    }

    async void SendMessage(string message)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.SendText(message);
            Debug.Log($"Sent message: {message}");
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Cannot send message.");
        }
    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }
}

// Helper class to run actions on the main thread
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly System.Collections.Generic.Queue<System.Action> executionQueue = new System.Collections.Generic.Queue<System.Action>();

    public static MainThreadDispatcher Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void RunOnMainThread(System.Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}