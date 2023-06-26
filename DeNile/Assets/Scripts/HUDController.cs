using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject GameHUD;
    public GameObject player;
    public GameObject mainCam;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        
    }

    public void callPauseMenu() //Pauses the game and enables the pause menu
    {
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        GameHUD.SetActive(false);
    }

    public void resumeGame() //Disables the pause menu and resumes the game
    {
        Time.timeScale = 1f;
        GameHUD.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void restartLevel() //Destroys everything that was set to not destroy on load as well as loads the current scene again
    {
        Destroy(player);
        Destroy(mainCam);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Destroy(gameObject);
    }
    public void quitToMainMenu() //Destroys every object on the scene and loads the main menu scene
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu Scene");
        Destroy(player);
        Destroy(mainCam);
        Destroy(gameObject);
    }
}
