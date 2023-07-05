using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using FishNet.Transporting.FishyUnityTransport;
using FishNet.Managing;

public class Temp : MonoBehaviour
{

    //FishyUTP:
    public NetworkManager networkManager;

    public async Task StartRelayHost()
    {
        //connect to relay first

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



    //Lobby:
    private Lobby hostLobby;
    private Lobby joinedLobby;

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new()
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Peaceful", DataObject.IndexOptions.S1) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = lobby;
            StartCoroutine(HandleLobbyHeartbeat());

            PrintPlayers(hostLobby);
            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private IEnumerator HandleLobbyHeartbeat()
    {
        while (hostLobby != null)
        {
            SendHeartbeat();
            yield return new WaitForSeconds(15);
        }
    }
    private async void SendHeartbeat()
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
    }
    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    //find lobbies with greater than 0 available slots
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //find lobbies with game mode equal to "Peaceful"
                    new QueryFilter(QueryFilter.FieldOptions.S1, "Peaceful", QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>
                {
                    //sort lobbies oldest to newest, descending
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies Found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void JoinFirstAvailableLobby()
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
            Debug.Log("Joined Lobby!");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new()
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);
            joinedLobby = lobby;
            Debug.Log("Joined Lobby with code " + lobbyCode);
            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void QuickJoinLobby() //if fails, causes error. I don't think I'll use this
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            //reset hostLobby so that heartbeat stops ticking
            hostLobby = null;
            joinedLobby = null;
            Debug.Log("Left Lobby!");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            //reset hostLobby so that heartbeat stops ticking
            hostLobby = null;
            joinedLobby = null;
            Debug.Log("Deleted Lobby!");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        string playerName = "azeTrom";

        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }
    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Player in lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value);
        foreach (var player in lobby.Players)
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
    }

    private async void UpdateLobbyGameMode(string newGameMode) //I shouldn't need this method!
    {
        try
        {
            //update hostLobby
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    //only updates this piece of data--other lobby data remains unchanged
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, newGameMode, DataObject.IndexOptions.S1) }
                }
            });
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}