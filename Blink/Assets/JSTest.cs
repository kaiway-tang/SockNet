using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class JSTest : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void AllInOne();

    [DllImport("__Internal")]
    private static extern void Hello();

    [DllImport("__Internal")]
    private static extern void HelloString(string str);

    void Start()
    {
        AllInOne();
        Hello();
        HelloString("This is a string.");
    }
}
