using DG.Tweening;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private const float FADE_DURATION = 0.25f;

    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    private bool isPaused;
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
        FadeIn();
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        FadeOut();
        isPaused = false;
        Time.timeScale = 1;
    }

    public void MainMenu()
    {
        Time.timeScale = 1;
        isPaused = false;
        sceneTransitionManager.ChangeScene("MainMenu");
    }

    private void FadeIn()
    {
        pauseCanvas.DOFade(1, FADE_DURATION).SetUpdate(true);
        pauseCanvas.interactable = true;
    }

    private void FadeOut()
    {
        pauseCanvas.DOFade(0, FADE_DURATION).SetUpdate(true);
        pauseCanvas.interactable = false;
    }
}
