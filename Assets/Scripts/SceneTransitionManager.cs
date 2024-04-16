using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private const float FADE_DURATION = 0.5f;

    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += (x, y) => FadeToClear();
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
        canvasGroup.alpha = 1;
        return canvasGroup.DOFade(0, 2 * FADE_DURATION);
    }
}
