using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : MonoBehaviour
{
    [SerializeField] Transform meshObj;
    [SerializeField] HPEntity hpScript;

    public void Init(Vector3 playerPos)
    {
        meshObj.position = playerPos;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        meshObj.position += (transform.position - meshObj.position) * 0.2f;
    }

    public void End()
    {
        hpScript.End();
    }
}
