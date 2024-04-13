using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Start()
    {
        gameOverCanvasGroup.alpha = 0;
        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;
    }

    public void DisplayGameOver(string score)
    {
        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;
        scoreText.text = $"You managed to get {score} !";
        gameOverCanvasGroup.DOFade(1, 0.25f).SetUpdate(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneTransitionManager.Instance.ChangeScene("Daycare");
    }

    public void MainMenu()
    {
        Time.timeScale = 1;
        SceneTransitionManager.Instance.ChangeScene("MainMenu");
    }
}
