using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{

    public Button loadGameButton;
    public TMP_Text loadGameText;

    private void Start()
    {
        Application.targetFrameRate = 60;
        if (PlayerPrefs.GetInt("CurrentLevel") == 0)
        {
            loadGameButton.interactable = false;
            loadGameText.alpha = .25f;
        }
        //BGM.Instance.Stop();
    }

    

    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(PlayerPrefs.GetInt("CurrentLevel"));
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
