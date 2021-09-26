using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles pausing behaviors
/// </summary>
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
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>
    /// Sets GameIsPaused to isPaused
    /// </summary>
    /// <param name="isPaused">Whether the game is paused</param>
    public void IsPaused(bool isPaused)
    {
        GameIsPaused = isPaused;
    }

    /// <summary>
    /// Closes pauseMenu, sets timeScale to 1, and locks mouse
    /// </summary>
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        controlsMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Opens pauseMenu, sets timeScale to 0, and unlocks mouse
    /// </summary>
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reloads current level
    /// </summary>
    public void Retry()
    {
        SceneManager.LoadScene(player.currentLevel);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Loads the next level
    /// </summary>
    public void NextLevel()
    {
        SceneManager.LoadScene(player.currentLevel + 1);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    /// <summary>
    /// Returns to the main menu
    /// </summary>
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }
}
