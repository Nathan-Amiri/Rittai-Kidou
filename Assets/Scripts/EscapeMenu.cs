using FishNet.Managing.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;

public class EscapeMenu : MonoBehaviour
{
    public static bool paused;

    //assigned in inspector
    public GameObject escapeCanvas;

    private bool eliminated;

    private void Update()
    {
        if (Input.GetButtonDown("EscapeMenu"))// && !eliminated)
            escapeCanvas.SetActive(!escapeCanvas.activeSelf);

        paused = escapeCanvas.activeSelf;

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    //public void GameEnd()
    //{
    //    eliminated = true;
    //    escapeCanvas.SetActive(true);
    //}

    public void LeaveMatch()
    {
        if (InstanceFinder.IsServer)
            InstanceFinder.ServerManager.StopConnection(false);
        else
            InstanceFinder.ClientManager.StopConnection();
    }
}