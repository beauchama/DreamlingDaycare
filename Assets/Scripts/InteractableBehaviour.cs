using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableBehaviour : MonoBehaviour
{
    public float InteractableDistance = 1f;
    public GameObject HoverText;
    public UnityEvent OnInteract;

    private void Start()
    {
        ShowInteractText(false);
    }

    private void Update()
    {
        if (Vector2.Distance(GameManager.Instance.Player.transform.position, transform.position) < InteractableDistance)
        {
            ShowInteractText(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnInteract.Invoke();
            }
        }
        else
        {
            ShowInteractText(false);
        }
    }

    private void ShowInteractText(bool show)
    {
        if (HoverText)
        {
            HoverText.SetActive(show);
        }
    }
}
