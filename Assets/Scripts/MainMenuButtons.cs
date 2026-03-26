using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour
{
    public void LoadTutorial()
    {
        SceneManager.LoadScene("TutorialMap");
    }
    public void LoadGame()
    {
        SceneManager.LoadScene("GameMap");
    }

    public void ExitGame()
    {
        Application.Quit();

        Debug.Log("Exit Button Pressed... Exiting Program");
    }
}
