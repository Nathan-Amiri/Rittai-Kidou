using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using UnityEngine.UI;
using System;
using FishNet.Object;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;

public class EscapeMenu : NetworkBehaviour
{
    public static bool paused { get; private set; }
    [NonSerialized] public int sensitivity;

    //assigned in scene
    public GameObject escapeCanvas;
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public TMP_Text pausedRespawnText;

    private GameManager gameManager;

    private float respawnTimeRemaining;
    private bool eliminated;

    private void OnEnable()
    {
        GameManager.OnClientConnectOrLoad += OnClientConnectOrLoad;
    }
    private void OnDisable()
    {
        GameManager.OnClientConnectOrLoad -= OnClientConnectOrLoad;
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("Sensitivity"))
            sensitivitySlider.value = PlayerPrefs.GetInt("Sensitivity");

        if (PlayerPrefs.HasKey("Volume"))
            volumeSlider.value = PlayerPrefs.GetInt("Volume");

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

        if (AudioListener.volume != (int)volumeSlider.value)
        {
            //volume increases as slider value approaches its max value (8)
            AudioListener.volume = volumeSlider.value / 8;
            PlayerPrefs.SetInt("Volume", (int)volumeSlider.value);
        }

        RespawnCooldown();
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        gameManager = gm;
    }

    public async void SelectLeaveMatch()
    {
        try
        {
            if (InstanceFinder.IsServer)
                await Lobbies.Instance.DeleteLobbyAsync(gameManager.lobby.Id);
            else
                await Lobbies.Instance.RemovePlayerAsync(gameManager.lobby.Id, AuthenticationService.Instance.PlayerId);

            if (IsServer)
                ServerManager.StopConnection(true);
            else
                ClientManager.StopConnection();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
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