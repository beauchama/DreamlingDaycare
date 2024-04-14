using System.Linq;
using TMPro;
using UnityEngine;

public class Mailbox : MonoBehaviour
{
    private float interactiveDistance;
    private Canvas interactiveContainer;
    private TextMeshProUGUI moneyText;

    private void Start()
    {
        interactiveContainer = GetComponentInChildren<Canvas>();
        moneyText = interactiveContainer.GetComponentsInChildren<TextMeshProUGUI>().First(n => n.name == "Money");
        moneyText.text = string.Empty;

        interactiveDistance = GetComponent<InteractableBehaviour>().InteractableDistance;
    }
    public void Interact()
    {
        var dreamling = GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>();

        if (dreamling)
        {
            GameManager.Instance.score.AddScore(dreamling.Score);
            Destroy(dreamling.gameObject);
            PlayerManager.Instance.CarriedDreamling = null;
        }
    }

    private void Update()
    {
        if (Vector2.Distance(GameManager.Instance.Player.transform.position, transform.position) < interactiveDistance)
        {
            interactiveContainer.gameObject.SetActive(true);

            var dreamling = GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>();

            if (dreamling)
                moneyText.text = $"Sell for {GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>().Score}$";
            else
                moneyText.text = string.Empty;

            return;
        }
        else
        {
            interactiveContainer.gameObject.SetActive(false);
        }
    }
}
