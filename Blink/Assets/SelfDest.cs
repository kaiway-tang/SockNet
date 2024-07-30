using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDest : MonoBehaviour
{
    [SerializeField] float time;
    void Start()
    {
        Destroy(gameObject, time);
    }
}
