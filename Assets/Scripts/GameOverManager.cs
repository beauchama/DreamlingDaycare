using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;

    private void Start()
    {
        gameOverCanvasGroup.alpha = 0;
    }

    public void DisplayGameOver(uint finalScore)
    {
        scoreText.text = $"You managed to get ${finalScore} !";
        gameOverCanvasGroup.DOFade(1, 0.25f).SetUpdate(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        sceneTransitionManager.ChangeScene("Daycare");
    }

    public void MainMenu()
    {
        Time.timeScale = 1;
        sceneTransitionManager.ChangeScene("MainMenu");
    }
}
