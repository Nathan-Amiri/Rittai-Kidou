using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using UnityEngine.UI;
using System;
using FishNet.Object;

public class EscapeMenu : NetworkBehaviour
{
    public static bool paused;
    [NonSerialized] public int sensitivity;

    //assigned in scene
    public GameObject escapeCanvas;
    public Slider sensitivitySlider;

    //private bool eliminated;

    private void Update()
    {
        if (Input.GetButtonDown("EscapeMenu"))// && !eliminated)
            escapeCanvas.SetActive(!escapeCanvas.activeSelf);

        paused = escapeCanvas.activeSelf;

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        sensitivity = (int)sensitivitySlider.value;
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