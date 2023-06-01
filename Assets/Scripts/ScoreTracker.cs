using FishNet.Object;
using FishNet.Object.Synchronizing;
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

    [SyncVar]
    private readonly int[] playerScores = new int[4];

    private void OnEnable()
    {
        GameManager.OnClientConnectOrLoad += OnClientConnectOrLoad;
        GameManager.OnRemoteClientDisconnect += OnRemoteClientDisconnect;
    }
    private void OnDisable()
    {
        GameManager.OnClientConnectOrLoad -= OnClientConnectOrLoad;
    }

    private void OnClientConnectOrLoad(GameManager gm)
    {
        gameManager = gm;
        ChangeScore(GameManager.playerNumber, 0, true);
    }

    private void OnRemoteClientDisconnect(int disconnectedPlayer)
    {
        //reset to 0 in case player reconnects
        ChangeScore(disconnectedPlayer, 0, true);
    }

    private void Start()
    {
        modeText.text = "Mode: " + (GameManager.peacefulGameMode ? "Peaceful" : "Battle");
    }

    [ServerRpc (RequireOwnership = false)]
    public void ChangeScore(int player, int amount, bool replace)
    {
        if (replace)
            playerScores[player - 1] = amount;
        else
            playerScores[player - 1] += amount;
        ClientChangeScore();
    }

    [ObserversRpc]
    private void ClientChangeScore()
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
                newText = gameManager.connectedPlayers[i] + ": " + playerScores[i] + you;
            }

            playerScoreTexts[i].text = newText;
        }
    }
}