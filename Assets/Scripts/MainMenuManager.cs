using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    private void Awake()
    {
        if (GameManager.Instance)
        {
            Destroy(GameManager.Instance.gameObject);
            GameManager.Instance = null;
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
