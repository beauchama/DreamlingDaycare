using UnityEngine;

public class Mailbox : MonoBehaviour
{
    public void Interact()
    {
        var dreamling = GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>();

        if (dreamling)
        {
            GameManager.Instance.score.AddScore(dreamling.Score);
            Destroy(dreamling.gameObject);
        }
    }
}
