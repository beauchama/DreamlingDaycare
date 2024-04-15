using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    private void Awake()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ResetGame();
        }
    }

    public void StartGame()
    {
        SceneTransitionManager.Instance.ChangeScene("Daycare");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
