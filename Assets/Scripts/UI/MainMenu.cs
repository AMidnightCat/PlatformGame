using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string SaveSceneName;
    public void PlayGame()
    {
        SceneManager.LoadScene("Save Manager");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
