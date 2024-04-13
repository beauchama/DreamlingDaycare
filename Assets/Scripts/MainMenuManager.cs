using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneTransitionManager.Instance.ChangeScene("Daycare Dom");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
