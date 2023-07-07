using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    //assigned in prefab:
    public AudioSource mainSource;
    public AudioSource windSource;
    public AudioSource reelSource;
    public AudioSource gasHoldSource;
    //0-2 fire, 3-4 release, 5-6 gas, 7 boost, 8 slam, 9 launch
    public List<AudioClip> clipList = new();
    public Rigidbody playerRb; //this class reads the velocity but does not affect the rb

    //dynamic:
    private bool soundEnabled;

    private void Start()
    {
        StartCoroutine(EnableSound());
    }
    public IEnumerator EnableSound() //called by Start and Player
    {
        soundEnabled = false;
        yield return new WaitForSeconds(.8f);
        windSource.Play();
        soundEnabled = true;
    }

    private void Update()
    {
        //increase wind volume as speed approaches 500
        windSource.volume = playerRb.velocity.magnitude / 500;
    }

    public void PlaySoundEffect(string soundEffect) //called by Player
    {
        if (!soundEnabled) return;

        switch (soundEffect)
        {
            case "Fire":
                mainSource.PlayOneShot(clipList[Random.Range(0, 3)]);
                break;
            case "Release":
                mainSource.PlayOneShot(clipList[Random.Range(3, 5)]);
                break;
            case "StartReel":
                reelSource.Play();
                break;
            case "EndReel":
                reelSource.Stop();
                break;
            case "Gas":
                mainSource.PlayOneShot(clipList[Random.Range(5, 7)]);
                gasHold = true;
                StartCoroutine(GasDelay());
                break;
            case "GasEnd":
                gasHoldSource.Stop();
                gasHold = false;
                break;
            case "Boost":
                mainSource.PlayOneShot(clipList[7]);
                break;
            case "Slam":
                mainSource.PlayOneShot(clipList[8]);
                break;
            case "Launch":
                //launch doesn't happen from missile when fired from player due to unity bug
                mainSource.PlayOneShot(clipList[9]);
                break;
            case "Stop":
                reelSource.Stop();
                gasHoldSource.Stop();
                break;
        }
    }

    //when pressing gas, gas sound plays, code waits for one second, then, if gas is still being held, it starts gasHold loop
    private bool gasHold;
    private IEnumerator GasDelay()
    {
        yield return new WaitForSeconds(1);
        if (gasHold)
            gasHoldSource.Play();
    }
}