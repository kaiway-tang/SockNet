using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : HPEntity
{
    public Vector3 lerpDest;
    [SerializeField] Transform meshObj;
    // Start is called before the first frame update
    void Start()
    {
        meshObj.parent = null;
        transform.position = lerpDest;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        meshObj.position += (transform.position - meshObj.position) * 0.2f;
    }
}
