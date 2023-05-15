using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    public static bool paused;
    public static bool practiceMode;

    //assigned in inspector
    public GameObject escapeCanvas;
    public TMP_Text resetText;
    public TMP_Text modeText;

    public Button resetButton;
    public Button modeButton;

    private bool gameOver;

    private void Start()
    {
        resetButton.interactable = false;
        modeButton.interactable = true;

        if (practiceMode)
            modeText.text = "Practice";
        else
            modeText.text = "Battle";
    }

    private void Update()
    {
        if (Input.GetButtonDown("EscapeMenu") && !gameOver)
        {
            escapeCanvas.SetActive(!escapeCanvas.activeSelf);
            resetButton.interactable = true;
            modeButton.interactable = false;
        }

        paused = escapeCanvas.activeSelf;

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    public void GameEnd()
    {
        gameOver = true;
        resetText.text = "Play Again";
        escapeCanvas.SetActive(true);
    }

    public void SelectMode()
    {
        practiceMode = !practiceMode;
        //practiceMode is static, so it retains its value after scenechange
        SceneManager.LoadScene(0);
    }

    public void Reset()
    {
        SceneManager.LoadScene(0);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}