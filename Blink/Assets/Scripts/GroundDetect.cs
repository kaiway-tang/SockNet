using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundDetect : MonoBehaviour
{    
    public int touchCount;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            touchCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            touchCount--;
        }
    }
}
