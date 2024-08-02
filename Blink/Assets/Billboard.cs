using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] Transform trfm;
    private void Start()
    {
        trfm = transform;
    }
    public void Update()
    {
        trfm.forward = PlayerController.self.camTrfm.position - trfm.position;
    }
}
