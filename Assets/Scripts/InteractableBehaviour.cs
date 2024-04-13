using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableBehaviour : MonoBehaviour
{
    public GameObject HoverText;
    public UnityEvent OnInteract;

    private void Start()
    {
        if (HoverText != null)
            HoverText.SetActive(false);
    }

    private void ShowInteractText(bool show)
    {
            HoverText?.SetActive(true);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (HoverText != null)
                HoverText.SetActive(true);
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (HoverText != null)
                HoverText.SetActive(false);
        }
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            OnInteract.Invoke();
        }
    }
}
