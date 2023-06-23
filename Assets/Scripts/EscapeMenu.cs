using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using UnityEngine.UI;
using System;
using FishNet.Object;
using TMPro;

public class EscapeMenu : NetworkBehaviour
{
    public static bool paused { get; private set; }
    [NonSerialized] public int sensitivity;

    //assigned in scene
    public GameObject escapeCanvas;
    public Slider sensitivitySlider;
    public TMP_Text pausedRespawnText;

    private float respawnTimeRemaining;
    private bool eliminated;

    private void Start()
    {
        if (PlayerPrefs.HasKey("Sensitivity"))
            sensitivitySlider.value = PlayerPrefs.GetInt("Sensitivity");

        paused = true;
    }

    private void Update()
    {
        if (eliminated)
        {
            paused = true;
            if (!escapeCanvas.activeSelf)
                escapeCanvas.SetActive(true);
        }
        else if (Input.GetButtonDown("EscapeMenu"))
        {
            paused = !paused;
            escapeCanvas.SetActive(paused);
        }

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        if (sensitivity != (int)sensitivitySlider.value)
        {
            sensitivity = (int)sensitivitySlider.value;
            PlayerPrefs.SetInt("Sensitivity", sensitivity);
        }

        RespawnCooldown();
    }

    public void SelectLeaveMatch()
    {
        if (InstanceFinder.IsServer)
            InstanceFinder.ServerManager.StopConnection(false);
        else
            InstanceFinder.ClientManager.StopConnection();
    }

    public void EliminateRespawn(bool eliminate, float respawnTime, float networkDelay) //called by player
    {
        //if eliminate = false, respawn. respawnTime and networkDelay only used for respawn
        if (eliminate)
        {
            eliminated = true;
            StartRespawnCooldown(respawnTime, networkDelay);
        }
        else //if respawn, unpause
        {
            eliminated = false;
            paused = !paused;
            escapeCanvas.SetActive(paused);
        }
    }
    private void StartRespawnCooldown(float respawnTime, float networkDelay)
    {
        //jump ahead in the cooldown timer based on networkDelay
        respawnTimeRemaining = respawnTime - networkDelay;
    }
    private void RespawnCooldown() //run in update
    {
        if (respawnTimeRemaining > 0) //round up time remaining
        {
            respawnTimeRemaining -= Time.deltaTime;
            pausedRespawnText.text = "Respawn in: " + Mathf.CeilToInt(respawnTimeRemaining).ToString();
        }
        else
        {
            respawnTimeRemaining = 0;
            pausedRespawnText.text = "Paused";
        }
    }
}