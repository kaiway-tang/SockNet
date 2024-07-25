using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class JSTest : MonoBehaviour
{
#if False
    [SerializeField] MeshRenderer cubeMesh;
    [SerializeField] Material red;

    [DllImport("__Internal")]
    private static extern void AllInOne();

    [DllImport("__Internal")]
    private static extern void Hello();

    [DllImport("__Internal")]
    private static extern void HelloString(string str);

    [DllImport("__Internal")]
    private static extern void Bounce();

    void Start()
    {
        Invoke("Ping", 1);
    }

    void Ping()
    {
        AllInOne();
        Hello();
        HelloString("This is a string.");
        Bounce();
    }

    private void JSM(string message)
    {
        if (message == "red")
        {
            cubeMesh.material = red;
        }        
    }
#endif
}
