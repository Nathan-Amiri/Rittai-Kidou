using FishNet;
using FishNet.Managing;
using FishNet.Transporting.FishyUnityTransport;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.VisualScripting;

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
    //public TMP_Dropdown regionDropdown;
    public TMP_Text errorText;

    //assigned in scene
    public NetworkManager networkManager;

    //dynamic:
    private string currentGameMode;
    private int gameModeInt;
    private readonly string[] gameModeIndex = new string[3];

    private string roomName;

    private Lobby currentLobby;

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
        ToggleButtonsInteractable(false);

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

        //if (PlayerPrefs.HasKey("Region"))
        //{
        //    regionDropdown.value = PlayerPrefs.GetInt("Region");
        //    Region newRegion = GetRegion(regionDropdown.value);
        //    transport.ConnectToRegion(newRegion);
        //}

        _ = ConnectToRelay();
    }

    private void Update()
    {
        modeText.text = "Game Mode: " + currentGameMode;

        if (roomNameField.text != roomName)
        {
            roomName = roomNameField.text;
            PlayerPrefs.SetString("RoomName", roomName);
        }
    }

    private async Task ConnectToRelay() //run in Start
    {
        errorText.text = "Connecting...";

        await UnityServices.InitializeAsync();

        Debug.Log(AuthenticationService.Instance.IsSignedIn);
        if (AuthenticationService.Instance.IsSignedIn) //true if returning to MenuScene while still connected to relay services
        {
            ToggleButtonsInteractable(true);
            errorText.text = "";
            return;
        }

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            ToggleButtonsInteractable(true);
            errorText.text = "";
        }
        catch
        {
            errorText.text = "Failed to connect. Check your internet connection, then restart the game";
        }
    }

    private void ToggleButtonsInteractable(bool interactable)
    {
        quickMatch.interactable = interactable;
        createRoom.interactable = interactable;
        joinRoom.interactable = interactable;
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        SwitchToGameScene(gm);
    }

    public void ChangeUsername()
    {
        PlayerPrefs.SetString("Username", usernameField.text);
    }

    private void UsernameError()
    {
        errorText.text = "Must choose a username!";
    }

    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        string playerName = usernameField.text;

        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    private IEnumerator HandleLobbyHeartbeat() //keep lobby active (lobbies are automatically hidden after 30 seconds of inactivity)
    {
        while (currentLobby != null)
        {
            SendHeartbeat();
            yield return new WaitForSeconds(15);
        }
    }
    private async void SendHeartbeat()
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
    }
    private async void CreateLobby(string newRoomName, string newGameMode)
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new()
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> //RoomName = S1, GameMode = S2
                {
                    //Unlike the technical (and meaningless) lobby name, RoomName is a public data value that is searchable. Rittai Kidou
                    //uses RoomName in place of a Lobby Code
                    { "RoomName", new DataObject(DataObject.VisibilityOptions.Public, newRoomName, DataObject.IndexOptions.S1) },
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, newGameMode, DataObject.IndexOptions.S2) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            currentLobby = lobby;
            StartCoroutine(HandleLobbyHeartbeat());

            if (newRoomName == "")
                Debug.Log("Created Public Lobby. Game Mode: " + lobby.Data["GameMode"].Value);
            else
                Debug.Log("Created Private Lobby named " + lobby.Data["RoomName"].Value + ". Game Mode: " + lobby.Data["GameMode"].Value);

            TurnOnClient(true);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void SelectQuickMatch()
    {
        if (usernamePlaceHolder.enabled)
        {
            UsernameError();
            return;
        }

        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 50,
                Filters = new List<QueryFilter>() //RoomName = S1, GameMode = S2
                {
                    //find lobbies with AvailableSlots greater than 0
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //find lobbies with RoomName blank (not private)
                    new QueryFilter(QueryFilter.FieldOptions.S1, "", QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>
                {
                    //sort lobbies oldest to newest, descending
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            if (currentGameMode != "Random")
            {
                //find lobbies with GameMode equal to currentGameMode
                QueryFilter gameModeFilter = new(QueryFilter.FieldOptions.S2, currentGameMode, QueryFilter.OpOptions.EQ); //RoomName = S1, GameMode = S2

                queryLobbiesOptions.Filters.Add(gameModeFilter);
            }

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            if (queryResponse.Results.Count > 0) //if a lobby is available
            {
                Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
                currentLobby = lobby;
                Debug.Log("Joined Lobby named " + lobby.Data["RoomName"].Value + ". Mode: " + lobby.Data["GameMode"].Value);

                TurnOnClient(false);
            }
            else
            {
                string gameMode = currentGameMode;
                if (gameMode == "Random")
                    gameMode = Random.Range(0, 2) == 0 ? "Peaceful" : "Battle";

                CreateLobby("", gameMode); //leave lobby name blank
            }

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
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

        CreateLobby(roomName.ToUpper(), currentGameMode);
    }

    public async void SelectJoinPrivateRoom()
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

        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 50,
                Filters = new List<QueryFilter>() //RoomName = S1, GameMode = S2
                {
                    //find lobbies with RoomName equal to roomName
                    new QueryFilter(QueryFilter.FieldOptions.S1, roomName.ToUpper(), QueryFilter.OpOptions.EQ)
                }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            if (queryResponse.Results.Count == 0)
            {
                errorText.text = "Private room not found. Check your room name and try again";
                return;
            }
            if (queryResponse.Results[0].AvailableSlots == 0)
            {
                errorText.text = "Room is already full!";
                return;
            }

            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
            currentLobby = lobby;
            Debug.Log("Joined Lobby named " + lobby.Data["RoomName"].Value + ". Mode: " + lobby.Data["GameMode"].Value);

            TurnOnClient(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void TurnOnClient(bool host)
    {
        errorText.text = "Loading Room...";
        ToggleButtonsInteractable(false);

        if (host)
        {
            if (networkManager == null) //true if returning to MenuScene, sincce the NetworkManager isn't destroyed on scene change
                networkManager = GameObject.FindWithTag("NetworkManager").GetComponent<NetworkManager>();

            var utp = (FishyUnityTransport)networkManager.TransportManager.Transport;

            // Setup HostAllocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(4);
            utp.SetRelayServerData(new RelayServerData(hostAllocation, "dtls"));

            // Start Server Connection
            networkManager.ServerManager.StartConnection();

            // Setup JoinAllocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            utp.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            // Start Client Connection
            networkManager.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("Join");
            //use joincode
        }
    }
    private void SwitchToGameScene(GameManager gm) //called in OnClientConnectOrLoad
    {
        gm.peacefulGameMode = currentLobby.Data["GameMode"].Value == "Peaceful"; //can't use currentGameMode in case it's "Random"
        gm.lobby = currentLobby;

        if (InstanceFinder.IsHost)
            gm.RequestSceneChange("GameScene");
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

    //public void SelectNewRegion()
    //{
    //    Region newRegion = GetRegion(regionDropdown.value);
    //    transport.ConnectToRegion(newRegion);
    //    PlayerPrefs.SetInt("Region", regionDropdown.value);
    //}
    //private Region GetRegion(int regionInt)
    //{
    //    if (regionInt == 0)
    //        return Region.USAWest;
    //    else if (regionInt == 1)
    //        return Region.Asia;
    //    else
    //        return Region.Europe;
    //}
}