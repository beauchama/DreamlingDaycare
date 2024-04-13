using DG.Tweening;
using Dreamlings.Characters;
using Dreamlings.Tools;
using System.Linq;
using TMPro;
using UnityEditor.Search;
using UnityEngine;

public class DreamlingCharacter : MonoBehaviour
{
    private bool isPickup;
    private Dreamling dreamling;
    private float timer;
    private Vector3 targetPosition;
    private Canvas statContainer;

    public float InteractableDistance = 1f;
    public float moveSpeed = 1f;
    public float changeInterval = 2f;
    public Sprite[] Sprites;

    // Start is called before the first frame update
    void Start()
    {
        dreamling = new Dreamling
        {
            Name = DreamlingNameGenerator.Generate(),
            NeededFood = NeededFoodGenerator.Generate(),
        };

        GetComponent<SpriteRenderer>().sprite = Sprites[Random.Range(0, Sprites.Length)];
        statContainer = GetComponentInChildren<Canvas>();

        timer = changeInterval;
        SetRandomTargetPosition();
        SetDreamlingStats();
    }

    // Update is called once per frame
    void Update()
    {
        if (isPickup)
        {
            statContainer.gameObject.SetActive(false);

            if (Input.GetKeyDown(KeyCode.G))
            {
                Drop();
            }

            return;
        }

        if (Vector2.Distance(GameManager.Instance.Player.transform.position, transform.position) < InteractableDistance)
            statContainer.gameObject.SetActive(true);
        else
            statContainer.gameObject.SetActive(false);

        Move();
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetRandomTargetPosition();
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SetRandomTargetPosition();
            timer = changeInterval;
        }
    }

    private void SetRandomTargetPosition()
    {
        targetPosition = transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }

    public void Interact()
    {
        if (!isPickup)
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        isPickup = true;

        transform.DOScale(0.5f, 0.5f);
        transform.parent = GameObject.FindGameObjectWithTag("Player").transform;

        transform.DOLocalMove(new Vector3(0.5f, 0.5f, 0f), 0.5f);
    }

    private void Drop()
    {
        isPickup = false;

        transform.DOScale(1f, 0.5f);
        transform.parent = null;
    }

    private void SetDreamlingStats()
    {
        if (!statContainer)
            return;

        var stats = statContainer.GetComponentsInChildren<TextMeshProUGUI>();

        SetText("Name", dreamling.Name);
        SetText("Food", dreamling.NeededFood.ToString());
        SetText("Quality", $"{dreamling.Quality} stars");

        void SetText(string stat, string value)
        {
            stats.FirstOrDefault(x => x.name == stat).text = value;
        }
    }
}
