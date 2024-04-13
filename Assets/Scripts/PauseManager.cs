using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;

    private CanvasGroup pauseCanvas;

    // Start is called before the first frame update
    void Start()
    {
        pauseCanvas = GetComponentInChildren<CanvasGroup>();
        pauseCanvas.alpha = 0;
        pauseCanvas.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseCanvas.DOFade(1, 0.5f).SetUpdate(true);
        pauseCanvas.interactable = true;
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        isPaused = false;
        pauseCanvas.DOFade(0, 0.5f).SetUpdate(true);
        pauseCanvas.interactable = false;
    }

    public void MainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
