﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject controlsMenuUI;
    public PlayerMovement player;

    public bool inTutorial = false;

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !inTutorial && !player.isDying)
        {
            if (GameIsPaused)
            {
                Resume();
            } else
            {
                Pause();
            }
        }
    }

    public void IsPaused(bool isPaused)
    {
        GameIsPaused = isPaused;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        controlsMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        SceneManager.LoadScene(player.currentLevel);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(player.currentLevel+1);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }
}
