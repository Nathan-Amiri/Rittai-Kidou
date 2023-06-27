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
    //0-2 fire, 3-4 release, 5-6 gas, 7 boost, 8 slam
    public List<AudioClip> clipList = new();
    public Rigidbody playerRb; //this class reads the velocity but does not affect the rb

    private void Start()
    {
        windSource.Play();
    }

    private void Update()
    {
        //increase wind volume as speed approaches 500
        windSource.volume = playerRb.velocity.magnitude / 500;
    }

    public void PlaySoundEffect(string soundEffect) //called by Player
    {
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
                gasHoldSource.Play();
                break;
            case "GasEnd":
                gasHoldSource.Stop();
                break;
            case "Boost":
                mainSource.PlayOneShot(clipList[7]);
                break;
            case "Slam":
                mainSource.PlayOneShot(clipList[8]);
                break;
        }
    }
}