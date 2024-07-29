using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : NetworkObject
{
    [SerializeField] float targetXRot, targetYRot;
    [SerializeField] float xRot, yRot;
    // Start is called before the first frame update
    new void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        xRot = transform.eulerAngles.x;
        yRot = transform.eulerAngles.y;

        Vector3 vect = new Vector3();
        vect = transform.eulerAngles;
        vect.y = Tools.RotationalLerp(vect.y, targetYRot, 0.05f);
        transform.eulerAngles = vect;
    }
}
