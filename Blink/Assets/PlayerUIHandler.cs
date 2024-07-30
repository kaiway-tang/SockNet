using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIHandler : MonoBehaviour
{
    public float mana;
    [SerializeField] SpriteRenderer[] manaDiamonds;
    [SerializeField] Transform manaBar;

    [SerializeField] Transform HPbar;

    Vector3 barVect = Vector3.one;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if (mana > lastMana + 0.01f)
        {
            barVect.x = mana % 20 / 20;
            manaBar.localScale = barVect;

            if (mana % 20 < lastMana % 20 || mana > lastMana + 19f)
            {
                UpdateManaDiamonds();
            }
            lastMana = mana;
        }
        else if (mana < lastMana - 0.01f)
        {
            barVect.x = mana % 20 / 20;
            manaBar.localScale = barVect;
            if (mana % 20 > lastMana % 20 || mana < lastMana - 19f)
            {
                UpdateManaDiamonds();
            }
            lastMana = mana;
        }
    }

    float lastMana;
    void UpdateManaDiamonds()
    {
        manaDiamonds[0].enabled = mana >= 20;
        manaDiamonds[1].enabled = mana >= 40;
        manaDiamonds[2].enabled = mana >= 60;
        manaDiamonds[3].enabled = mana >= 80;
        manaDiamonds[4].enabled = mana >= 99.99;
    }

    public void SetHP(float hp)
    {
        barVect.x = hp / 100;
        HPbar.localScale = barVect;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
