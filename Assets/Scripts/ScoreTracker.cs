using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreTracker : NetworkBehaviour
{
    //assigned in scene
    public TMP_Text modeText;
    public TMP_Text[] playerScoreTexts = new TMP_Text[4];

    private GameManager gameManager;

    private readonly int[] playerScores = new int[4]; //server only

    private void OnEnable()
    {
        GameManager.OnClientConnectOrLoad += OnClientConnectOrLoad;
        GameManager.OnRemoteClientDisconnect += OnRemoteClientDisconnect;
    }
    private void OnDisable()
    {
        GameManager.OnClientConnectOrLoad -= OnClientConnectOrLoad;
        GameManager.OnRemoteClientDisconnect -= OnRemoteClientDisconnect;
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        gameManager = gm;
        RpcChangeScore(GameManager.playerNumber, 0, true);

        modeText.text = "Mode: " + (gameManager.peacefulGameMode ? "Peaceful" : "Battle");
    }

    private void OnRemoteClientDisconnect(int disconnectedPlayer)
    {
        //reset to 0 in case player reconnects
        RpcChangeScore(disconnectedPlayer, 0, true);
    }

    [ServerRpc (RequireOwnership = false)]
    public void RpcHalveScore(int player) //called by Player
    {
        playerScores[player - 1] = Mathf.RoundToInt(playerScores[player - 1] / 2);
        RpcClientChangeScore(playerScores);
    }

    [ServerRpc (RequireOwnership = false)]
    public void RpcChangeScore(int player, int amount, bool replace) //called by Player
    {
        if (replace)
            playerScores[player - 1] = amount;
        else
            playerScores[player - 1] += amount;
        RpcClientChangeScore(playerScores);
    }

    [ObserversRpc]
    private void RpcClientChangeScore(int[] newPlayerScores)
    {
        for (int i = 0; i < playerScoreTexts.Length; i++)
        {
            string newText;

            //if player isn't connected
            if (gameManager.connectedPlayers[i] == "")
                newText = "";
            else
            {
                string you = GameManager.playerNumber == i + 1 ? " (You)" : "";
                newText = gameManager.connectedPlayers[i] + ": " + newPlayerScores[i] + you;
            }

            playerScoreTexts[i].text = newText;
        }
    }
}