using DG.Tweening;
using TMPro;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public TextMeshProUGUI errorText;
    public CanvasGroup canvasGroup;

    private bool isDisplaying;

    public void DisplayError(string message)
    {
        if (isDisplaying)
            return;

        CancelInvoke(nameof(HideError));

        errorText.text = message;
        canvasGroup.alpha = 0;
        gameObject.SetActive(true);
        canvasGroup.DOFade(1, 0.5f).WaitForCompletion();
        isDisplaying = true;

        Invoke(nameof(HideError), 3f);
    }

    public void HideError()
    {
        canvasGroup.DOFade(0, 0.5f).OnComplete(
            () =>
            {
                isDisplaying = false;
                gameObject.SetActive(false);
            });
    }
}
