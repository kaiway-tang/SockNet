using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIHandler : UIHandler
{
    [SerializeField] SpriteRenderer[] manaDiamonds;
    [SerializeField] Transform manaBar;

    // Start is called before the first frame update

    public override void Init(int pMaxHP)
    {
        base.Init(pMaxHP);
    }

    int mana, lastMana;
    public void SetMana(int value)
    {
        mana = value;
        if (mana > lastMana)
        {
            barVect.x = mana % 200 / 200f;
            manaBar.localScale = barVect;

            if (mana % 20 < lastMana % 200 || mana > lastMana + 198)
            {
                UpdateManaDiamonds();
            }
            lastMana = mana;
        }
        else if (mana < lastMana)
        {
            barVect.x = mana % 200 / 200f;
            manaBar.localScale = barVect;
            if (mana % 20 > lastMana % 200 || mana < lastMana - 198)
            {
                UpdateManaDiamonds();
            }
            lastMana = mana;
        }
    }

    void UpdateManaDiamonds()
    {
        manaDiamonds[0].enabled = mana >= 200;
        manaDiamonds[1].enabled = mana >= 400;
        manaDiamonds[2].enabled = mana >= 600;
        manaDiamonds[3].enabled = mana >= 800;
        manaDiamonds[4].enabled = mana >= 998;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
