using TMPro;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public TextMeshProUGUI errorText;

    public void DisplayError(string message)
    {
        errorText.text = message;
        gameObject.SetActive(true);
    }

    public void HideError()
    {
        gameObject.SetActive(false);
    }
}
