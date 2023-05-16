using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ScoreTracker : MonoBehaviour
{
    public static int currentScore = 0;

    //assigned in inspector
    public TMP_Text currentScoreText;
    public TMP_Text highScoreText;

    private int highScore;

    private void Start()
    {
        //reset when scene reloads
        currentScore = 0;

        //get saved high score
        if (PlayerPrefs.HasKey("HighScore"))
            highScore = PlayerPrefs.GetInt("HighScore");
    }

    private void Update()
    {
        if (EscapeMenu.practiceMode)
        {
            currentScoreText.text = "";
            highScoreText.text = "";
            return;
        }

        currentScoreText.text = "Score: " + currentScore;
        highScoreText.text = "High Score: " + highScore;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", currentScore);
        }
    }
}