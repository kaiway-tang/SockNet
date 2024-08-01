using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] AudioSource run;
    [SerializeField] AudioSource slash;
    [SerializeField] AudioSource shurikenThrow;
    
    public void PlaySlash()
    {
        slash.Play();
    }
    public void PlayShurikenThrow()
    {
        shurikenThrow.Play();
    }

    public void ToggleRun(bool on)
    {
        if (run.isPlaying != on)
        {
            if (on) { run.Play(); }
            else { run.Stop(); }
        }
    }
}
