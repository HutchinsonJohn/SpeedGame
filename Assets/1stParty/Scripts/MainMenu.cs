using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Menu functions
/// </summary>
public class MainMenu : MonoBehaviour
{

    public Button levelSelectButton;
    public TMP_Text levelSelectText;
    public GameObject level2Button;
    public GameObject level3Button;
    public TMP_Text level1Time;
    public TMP_Text level2Time;
    public TMP_Text level3Time;

    private void Start()
    {
        Application.targetFrameRate = 60;
        if (PlayerPrefs.HasKey("Level1BestTime"))
        {
            level1Time.text = PlayerMovement.FormatTime(PlayerPrefs.GetFloat("Level1BestTime"));
            if (PlayerPrefs.HasKey("Level2BestTime"))
            {
                level2Time.text = PlayerMovement.FormatTime(PlayerPrefs.GetFloat("Level2BestTime"));
                if (PlayerPrefs.HasKey("Level3BestTime"))
                {
                    level3Time.text = PlayerMovement.FormatTime(PlayerPrefs.GetFloat("Level3BestTime"));
                }
                else
                {
                    level3Button.SetActive(false);
                }
            }
            else
            {
                level2Button.SetActive(false);
            }
        }
        else
        {
            levelSelectButton.interactable = false;
            levelSelectText.alpha = .25f;
        }
        
        //BGM.Instance.Stop();
    }

    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }

    public void LoadGame(int selectedLevel)
    {
        SceneManager.LoadScene(selectedLevel);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
