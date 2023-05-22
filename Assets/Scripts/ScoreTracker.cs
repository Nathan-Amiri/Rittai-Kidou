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
        ChangeScore(GameManager.playerNumber, 0);
    }

    private void OnRemoteClientDisconnect(int disconnectedPlayer)
    {
        //not actually setting disconnected player's score to 0
        //(since that player's score will be deleted)
        //merely updating the scores
        ChangeScore(disconnectedPlayer, 0);
    }

    private void Start()
    {
        modeText.text = "Mode: " + (GameManager.peacefulGameMode ? "Peaceful" : "Battle");
    }

    [ServerRpc (RequireOwnership = false)]
    public void ChangeScore(int player, int amount)
    {
        playerScores[player - 1] = amount;
        ClientChangeScore();
    }

    [ObserversRpc]
    private void ClientChangeScore()
    {
        for (int i = 0; i < playerScoreTexts.Length; i++)
        {
            string newText;

            //if player isn't connected
            if (gameManager.connectedPlayers[i] == 0)
                newText = "";
            else
            {
                string you = GameManager.playerNumber == i + 1 ? " (You)" : "";
                newText = "Player " + (i + 1) + ": " + playerScores[i] + you;
            }

            playerScoreTexts[i].text = newText;
        }
    }
}