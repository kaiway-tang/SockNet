using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField] protected Transform HPbar;
    [SerializeField] protected int maxHP;
    protected Vector3 barVect = Vector3.one;
    // Start is called before the first frame update
    protected void Start()
    {
        
    }

    public virtual void Init(int pMaxHP)
    {
        maxHP = pMaxHP;
    }

    public virtual void SetHP(float hp)
    {
        barVect.x = hp / maxHP;
        HPbar.localScale = barVect;        
    }
}
