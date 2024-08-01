using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform trfm;
    private void Start()
    {
        trfm = transform;
    }
    private void Update()
    {
        trfm.forward = PlayerController.self.camTrfm.position - trfm.position;
    }
}
