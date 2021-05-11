using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LoadTrigger : MonoBehaviour
{

    public int levelToLoad;

    public PlayerMovement player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerPrefs.SetInt("CurrentLevel", levelToLoad);
            PlayerPrefs.Save();

            SceneManager.LoadScene(levelToLoad);
        }
    }

}
