using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools : MonoBehaviour
{
    public static int hurtboxMask = 1 << 6;
    public static int terrainMask = 1 << 8;
    public static float RotationalLerp(float start, float dest, float rate)
    {
        if (Mathf.Abs(dest - start) < 180)
        {
            return start + (dest - start) * rate;
        }
        else
        {
            if (dest > start)
            {
                return (start + (dest - start - 360) * rate);
            }
            else
            {
                return (start + (360 - start + dest) * rate) % 360;
            }

        }
    }

    static float xBoxDist, yBoxDist, zBoxDist;
    public static float BoxDist(Vector3 posA, Vector3 posB)
    {
        xBoxDist = posA.x - posB.x;
        yBoxDist = posA.y - posB.y;
        zBoxDist = posA.z - posB.z;

        if (xBoxDist > yBoxDist)
        {
            return xBoxDist > zBoxDist ? xBoxDist : zBoxDist;
        }
        else
        {
            return yBoxDist > zBoxDist ? yBoxDist : zBoxDist;
        }
    }

    public static uint RandomEventID()
    {
        return (uint)Random.Range(-2147483647, 2147483647) + 2147483648;
    }
}
