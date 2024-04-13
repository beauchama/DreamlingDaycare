using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private SceneTransitionManager sceneTransitionManager;

    public void StartGame()
    {
        sceneTransitionManager.ChangeScene("Daycare");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
