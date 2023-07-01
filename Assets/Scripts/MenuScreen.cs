using FishNet;
using FishyRealtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScreen : MonoBehaviour
{
    //assigned in prefab
    public TMP_InputField usernameField;
    public TextMeshProUGUI usernamePlaceHolder;
    public TMP_Text modeText;
    public TMP_InputField roomNameField;
    public TextMeshProUGUI roomNamePlaceholder;
    public Button quickMatch;
    public Button createRoom;
    public Button joinRoom;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown regionDropdown;
    public TMP_Text errorText;

    //assigned in scene
    public FishyRealtime.FishyRealtime transport;

    private string currentGameMode;
    private int gameModeInt;
    private readonly string[] gameModeIndex = new string[3];

    private string roomName;

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
        gameModeIndex[0] = "Peaceful";
        gameModeIndex[1] = "Battle";
        gameModeIndex[2] = "Random";

        if (PlayerPrefs.HasKey("GameMode"))
            gameModeInt = PlayerPrefs.GetInt("GameMode");
        else
            gameModeInt = 0; //peaceful is default
        currentGameMode = gameModeIndex[gameModeInt];

        if (PlayerPrefs.HasKey("Username"))
            usernameField.text = PlayerPrefs.GetString("Username");

        if (PlayerPrefs.HasKey("RoomName"))
            roomNameField.text = PlayerPrefs.GetString("RoomName");

        if (PlayerPrefs.HasKey("Resolution"))
            resolutionDropdown.value = PlayerPrefs.GetInt("Resolution");

        if (PlayerPrefs.HasKey("Region"))
        {
            regionDropdown.value = PlayerPrefs.GetInt("Region");
            Region newRegion = GetRegion(regionDropdown.value);
            transport.ConnectToRegion(newRegion);
        }
    }

    private void Update()
    {
        modeText.text = "Game Mode: " + currentGameMode;

        if (roomNameField.text != roomName)
        {
            roomName = roomNameField.text;
            PlayerPrefs.SetString("RoomName", roomName);
        }

        bool interactable = FishyRealtime.FishyRealtime.isConnectedToMaster;
        quickMatch.interactable = interactable;
        createRoom.interactable = interactable;
        joinRoom.interactable = interactable;
        if (!interactable)
            errorText.text = "Connecting...";
        else if (errorText.text == "Connecting...")
            errorText.text = "";
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        bool peaceful;
        if (currentGameMode == "Random")
            peaceful = Random.Range(0, 2) == 0;
        else
            peaceful = currentGameMode == "Peaceful";

        gm.roomName = roomName;

        gm.peacefulGameMode = peaceful;
        if (InstanceFinder.IsHost)
            gm.RequestSceneChange("GameScene");
    }

    public void ChangeUsername()
    {
        PlayerPrefs.SetString("Username", usernameField.text);
    }

    private void UsernameError()
    {
        errorText.text = "Must choose a username!";
    }

    public void SelectQuickMatch()
    {
        if (usernamePlaceHolder.enabled)
        {
            UsernameError();
            return;
        }

        if (currentGameMode == "Random")
            transport.JoinRandomRoom(true);
        else
        {
            RoomFilter filter = new()
            {
                gameMode = currentGameMode
            };
            transport.JoinRandomRoom(filter, true);
        }
    }

    public void SelectCreatePrivateRoom()
    {
        if (usernamePlaceHolder.enabled)
        {
            UsernameError();
            return;
        }
        if (currentGameMode == "Random")
        {
            errorText.text = "Must specify a game mode for your private room!";
            return;
        }
        if (roomNamePlaceholder.enabled)
        {
            errorText.text = "Must choose a name for your private room!";
            return;
        }

        FishyRealtime.Room info = new()
        {
            name = roomName.ToLower(),
            maxPlayers = 3,
            isPublic = false,
            open = true
        };
        RoomFilter filter = new()
        {
            gameMode = currentGameMode
        };

        transport.CreateRoom(info, filter);
    }

    public void SelectJoinPrivateRoom()
    {
        if (usernamePlaceHolder.enabled)
        {
            UsernameError();
            return;
        }
        if (roomNamePlaceholder.enabled)
        {
            errorText.text = "Must provide the name of the private room you'd like to join!";
            return;
        }

        transport.JoinRoom(roomName);
    }

    public void SelectChangeMode()
    {
        gameModeInt++;
        if (gameModeInt > 2)
            gameModeInt = 0;

        currentGameMode = gameModeIndex[gameModeInt];

        PlayerPrefs.SetInt("GameMode", gameModeInt);
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
        PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);
    }

    public void SelectNewRegion()
    {
        Region newRegion = GetRegion(regionDropdown.value);
        transport.ConnectToRegion(newRegion);
        PlayerPrefs.SetInt("Region", regionDropdown.value);
    }
    private Region GetRegion(int regionInt)
    {
        if (regionInt == 0)
            return Region.USAWest;
        else if (regionInt == 1)
            return Region.Asia;
        else
            return Region.Europe;
    }
}