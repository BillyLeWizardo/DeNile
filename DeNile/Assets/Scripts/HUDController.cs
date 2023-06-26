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

    public void callPauseMenu()
    {
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        GameHUD.SetActive(false);
    }

    public void resumeGame()
    {
        Time.timeScale = 1f;
        GameHUD.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void restartLevel()
    {
        Destroy(player);
        Destroy(mainCam);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Destroy(gameObject);
    }
    public void quitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu Scene");
        Destroy(player);
        Destroy(mainCam);
        Destroy(gameObject);
    }
}
