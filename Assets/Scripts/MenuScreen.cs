using FishNet;
using FishNet.Transporting.Tugboat;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class MenuScreen : MonoBehaviour
{
    //assigned in prefab
    public TMP_InputField usernameField;
    public TextMeshProUGUI usernamePlaceHolder;
    public TMP_Text modeText;
    public Button startLobby;
    public TMP_InputField ipAddressField;
    public TextMeshProUGUI ipPlaceHolder;
    public Button joinLobby;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Text errorText;

    //assigned in scene
    public Tugboat tugboat;

    private bool peacefulGameMode = true;

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
        if (PlayerPrefs.HasKey("Username"))
            usernameField.text = PlayerPrefs.GetString("Username");
    }

    private void Update()
    {
        startLobby.interactable = !usernamePlaceHolder.enabled;
        joinLobby.interactable = !ipPlaceHolder.enabled && !usernamePlaceHolder.enabled;
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        GameManager.peacefulGameMode = peacefulGameMode;
        gm.SceneChange("GameScene");
    }

    public void ChangeUsername()
    {
        PlayerPrefs.SetString("Username", usernameField.text);
    }

    public void SelectChangeMode()
    {
        peacefulGameMode = !peacefulGameMode;
        modeText.text = "Mode: " + (peacefulGameMode ? "Peaceful" : "Battle");
    }

    public void SelectStartLobby()
    {
        InstanceFinder.ServerManager.StartConnection();
    }

    public void SelectJoinLobby()
    {
        tugboat.SetClientAddress(ipAddressField.text);
        InstanceFinder.ClientManager.StartConnection();
    }

    public void SelectExitGame()
    {
        Application.Quit();
    }

    public void SelectNewResolution()
    {
        switch (resolutionDropdown.value)
        {
            case 0:
                Screen.SetResolution(1920, 1080, true);
                break;
            case 1:
                Screen.SetResolution(1280, 720, true);
                break;
            case 2:
                Screen.SetResolution(1366, 768, true);
                break;
            case 3:
                Screen.SetResolution(1600, 900, true);
                break;
        }
    }
}