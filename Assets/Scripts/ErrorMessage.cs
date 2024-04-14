using TMPro;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public TextMeshProUGUI errorText;

    public void DisplayError(string message)
    {
        CancelInvoke(nameof(HideError));

        errorText.text = message;
        gameObject.SetActive(true);

        Invoke(nameof(HideError), 3f);
    }

    public void HideError()
    {
        gameObject.SetActive(false);
    }
}
