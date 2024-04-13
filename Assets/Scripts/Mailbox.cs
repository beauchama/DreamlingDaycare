using UnityEngine;

public class Mailbox : MonoBehaviour
{
    public float InteractableDistance = 1f;

    public void Start()
    {
        InteractableDistance = GetComponent<InteractableBehaviour>().InteractableDistance;
    }

    public void Interact()
    {
        var dreamling = GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>();

        if (dreamling)
            Destroy(dreamling.gameObject);
    }
}
