using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public void BackToMenu()
    {
        SceneTransitionManager.Instance.ChangeScene("MainMenu");
    }
}