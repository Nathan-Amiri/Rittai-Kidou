using FishNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Connection;
using FishNet.Object;
using TMPro;

public class Setup : NetworkBehaviour
{
    //assigned in scene
    public GameObject playerPref;

    public Camera mainCamera;

    public TMP_Text leftCrosshair;
    public TMP_Text rightCrosshair;

    public Transform gasScaler;
    public Image gasAmountImage;

    public Transform missileScaler;
    public Image missileAmountImage;

    public MissileLauncher missileLauncher;
    public EscapeMenu escapeMenu;
    public ScoreTracker scoreTracker;

    public List<GameObject> hearts = new();

    public List<Color> playerColors = new();

    //assigned dynamically
    private GameManager gameManager;

    private void OnEnable()
    {
        GameManager.OnClientConnectOrLoad += OnSpawn;
    }
    private void OnDisable()
    {
        GameManager.OnClientConnectOrLoad -= OnSpawn;
    }

    private void OnSpawn(GameManager gm)
    {
        gameManager = gm;

        Vector3 temporarySpawnPosition = new(0, 10, 0);
        Color playerColor = playerColors[GameManager.playerNumber - 1];
        RpcSpawnPlayer(InstanceFinder.ClientManager.Connection, GameManager.playerNumber, temporarySpawnPosition, playerColor);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcSpawnPlayer(NetworkConnection conn, int newPlayerNumber, Vector3 newPlayerPosition, Color playerColor)
    {
        GameObject newPlayerObject = Instantiate(playerPref, newPlayerPosition, Quaternion.identity);

        //rigidbody only needs to be added on the server
        gameManager.playerRbs[newPlayerNumber - 1] = newPlayerObject.GetComponent<Rigidbody>();

        InstanceFinder.ServerManager.Spawn(newPlayerObject, conn);
        RpcStartPlayer(newPlayerObject, playerColor);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcStartPlayer(GameObject newPlayerObject, Color playerColor)
    {
        if (newPlayerObject == null) return; //if this player has already disconnected

        Player newPlayer = newPlayerObject.GetComponent<Player>();
        newPlayer.mainCamera = mainCamera;
        newPlayer.leftCrosshair = leftCrosshair;
        newPlayer.rightCrosshair = rightCrosshair;
        newPlayer.gasScaler = gasScaler;
        newPlayer.gasAmountImage = gasAmountImage;
        newPlayer.missileScaler = missileScaler;
        newPlayer.missileAmountImage = missileAmountImage;
        newPlayer.missileLauncher = missileLauncher;
        newPlayer.escapeMenu = escapeMenu;
        newPlayer.scoreTracker = scoreTracker;
        newPlayer.hearts = hearts;

        newPlayer.OnSpawn(playerColor);
    }
}