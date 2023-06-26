using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void TutorialPlay()
    {
        SceneManager.LoadScene("Tutorial Level");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}