using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] AudioSource run;
    [SerializeField] AudioSource slash;
    [SerializeField] AudioSource shurikenThrow;
    [SerializeField] AudioSource enterVanish;
    [SerializeField] AudioSource exitVanish;

    [SerializeField] AudioSource[] drumTaps;

    public void PlaySlash()
    {
        slash.Play();
    }
    public void PlayShurikenThrow()
    {
        shurikenThrow.Play();
    }

    public void PlayEnterVanish()
    {
        enterVanish.Play();
    }

    public void PlayExitVanish()
    {
        exitVanish.Play();
    }

    public void PlayDrumTap(int index)
    {
        drumTaps[index].Play();
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
