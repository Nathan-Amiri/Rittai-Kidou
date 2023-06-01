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

    public ObjectPool objectPool;
    public EscapeMenu escapeMenu;
    public ScoreTracker scoreTracker;

    public List<GameObject> hearts = new();

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
        SpawnPlayer(InstanceFinder.ClientManager.Connection, GameManager.playerNumber, temporarySpawnPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer(NetworkConnection conn, int newPlayerNumber, Vector3 newPlayerPosition)
    {
        GameObject newPlayerObject = Instantiate(playerPref, newPlayerPosition, Quaternion.identity);
        InstanceFinder.ServerManager.Spawn(newPlayerObject, conn);
        RpcStartPlayer(newPlayerObject, conn);
    }

    [ObserversRpc(BufferLast = true)] //bufferlast is needed because this rpc is run on clients that may not have received the beacon signal yet
    private void RpcStartPlayer(GameObject newPlayerObject, NetworkConnection conn)
    {
        Player newPlayer = newPlayerObject.GetComponent<Player>();
        newPlayer.mainCamera = mainCamera;
        newPlayer.leftCrosshair = leftCrosshair;
        newPlayer.rightCrosshair = rightCrosshair;
        newPlayer.gasScaler = gasScaler;
        newPlayer.gasAmountImage = gasAmountImage;
        newPlayer.missileScaler = missileScaler;
        newPlayer.missileAmountImage = missileAmountImage;
        newPlayer.objectPool = objectPool;
        newPlayer.escapeMenu = escapeMenu;
        newPlayer.scoreTracker = scoreTracker;

        newPlayer.OnSpawn();
    }
}