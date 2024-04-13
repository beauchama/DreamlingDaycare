using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private const float FADE_DURATION = 0.5f;

    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup.alpha = 1;
        FadeToClear();
    }

    public void ChangeScene(string sceneName)
    {
        FadeToBlack().OnComplete(
            () =>
            {
                SceneManager.LoadScene(sceneName);
            });
    }

    private Tween FadeToBlack()
    {
        return canvasGroup.DOFade(1, FADE_DURATION).SetUpdate(true);
    }

    private Tween FadeToClear()
    {
        return canvasGroup.DOFade(0, 2 * FADE_DURATION).SetUpdate(true);
    }
}
