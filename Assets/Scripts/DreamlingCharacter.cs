using DG.Tweening;
using Dreamlings.Characters;
using Dreamlings.Tools;
using System.Linq;
using Dreamlings.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class DreamlingCharacter : MonoBehaviour
{
    private bool isPickup;
    private Dreamling dreamling;
    private float timer;
    private Vector3 targetPosition;
    private Canvas statContainer;
    private Tween MoveTween;
    private SpriteRenderer spriteRenderer;

    public float InteractableDistance = 1f;
    public float moveSpeed = 1f;
    public float changeInterval = 2f;
    public Sprite[] Sprites;
    public GameObject[] Prefabs;
    public GameObject Baby;
    public BreedTimer BreedTimer;

    public int Score => dreamling.Score;

    public Dreamling OverriddenDreamling;
    public bool OverrideCarry;

    // Start is called before the first frame update
    void Start()
    {
        timer = changeInterval;
        statContainer = GetComponentInChildren<Canvas>();
        BreedTimer = GetComponent<BreedTimer>();

        if (OverriddenDreamling != null)
        {
            dreamling = OverriddenDreamling;
            //spriteRenderer = GetComponent<SpriteRenderer>();
            //spriteRenderer.sprite = Sprites[(int)dreamling.DreamlingType];
            for (int i = 0; i < Prefabs.Length; i++)
            {
                Prefabs[i].SetActive(i == (int)dreamling.DreamlingType);
            }

            SetDreamlingStats();

            if (OverrideCarry)
            {
                isPickup = true;

                transform.DOScale(0.75f, 0f);
                ChangeSortOrder(11);
                transform.parent = GameManager.Instance.Player.transform;

                transform.DOLocalMove(new Vector3(0.5f, 0.5f, 0f), 0f);
                GetComponent<InteractableBehaviour>().enabled = false;
                PlayerManager.Instance.CarriedDreamling = dreamling;
            }

            return;
        }

        if (dreamling == null)
        {
            dreamling = new Dreamling
            {
                Name = DreamlingNameGenerator.Generate(),
                NeededFood = NeededFoodGenerator.Generate(),
                DreamlingType = DreamlingTypeGenerator.Generate(),
            };

            //GetComponent<SpriteRenderer>().sprite = Sprites[(int)dreamling.DreamlingType];
            for (int i = 0; i < Prefabs.Length; i++)
            {
                Prefabs[i].SetActive(i == (int)dreamling.DreamlingType);
            }
        }

        SetRandomTargetPosition();
        SetDreamlingStats();
    }

    // Update is called once per frame
    void Update()
    {
        if (isPickup)
        {
            statContainer.gameObject.SetActive(false);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Drop();
            }

            return;
        }

        if (Vector2.Distance(GameManager.Instance.Player.transform.position, transform.position) < InteractableDistance)
        {
            statContainer.gameObject.SetActive(true);
            return;
        }
        else
        {
            statContainer.gameObject.SetActive(false);
        }

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
        if (!isPickup && !GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>())
        {
            Pickup();
            return;
        }

        if (dreamling.CanBreed() && !BreedTimer.IsRunning)
        {
            var dreamlingToBreed = GameManager.Instance.Player.GetComponentInChildren<DreamlingCharacter>();

            if (dreamlingToBreed.dreamling.CanBreed() && !dreamlingToBreed.BreedTimer.IsRunning)
            {
                Breed(dreamlingToBreed);
            }
        }
        else
        {
            Debug.Log("Dreamling can't breed");
        }
    }

    private void ChangeSortOrder(int order)
    {
        foreach(GameObject prefab in Prefabs)
        {
            var sr = prefab.GetComponent<SpriteRenderer>();
            var sg = prefab.GetComponent<SortingGroup>();
            if (sr)
            {
                sr.sortingOrder = order;
            }
            if (sg)
            {
                sg.sortingOrder = order;
            }
        }
    }

    private void Pickup()
    {
        isPickup = true;

        GetComponent<Rigidbody2D>().simulated = false;
        ChangeSortOrder(11);
        transform.DOScale(0.75f, 0.5f);
        transform.parent = GameManager.Instance.Player.transform;

        MoveTween = transform.DOLocalMove(new Vector3(0.5f, 0.5f, 0f), 0.5f);

        GetComponent<InteractableBehaviour>().enabled = false;

        PlayerManager.Instance.CarriedDreamling = dreamling;
        GameManager.Instance.RemoveDreamlingFromBarn(dreamling);
    }

    private void Drop()
    {
        var success = GameManager.Instance.AddDreamlingToBarn(dreamling);
        if (!success)
            return;

        isPickup = false;

        MoveTween.Kill();
        GetComponent<Rigidbody2D>().simulated = true;
        ChangeSortOrder(5);
        transform.DOScale(1f, 0.5f);
        transform.parent = null;
        GetComponent<InteractableBehaviour>().enabled = true;

        PlayerManager.Instance.CarriedDreamling = null;
    }

    private void Breed(DreamlingCharacter otherParent)
    {
        var parentBarn = GameManager.Instance.GetDreamlingBarn(dreamling);
        if (parentBarn == null)
        {
            GameManager.Instance.errorMessageDisplay.DisplayError("The Dreamling must be in a barn to breed.");
            return;
        }

        if (parentBarn.IsFull)
        {
            GameManager.Instance.errorMessageDisplay.DisplayError("The barn is full! Choose a different barn or sell a Dreamling.");
            return;
        }

        BreedTimer.StartTimer();
        otherParent.BreedTimer.StartTimer();

        var baby = dreamling.Breed();

        var babyInstance = Instantiate(Baby, GameManager.Instance.Player.transform.position, Quaternion.identity);
        babyInstance.transform.DOScale(1f, 0.5f);

        baby.DreamlingType = GetDreamlingType(dreamling.DreamlingType, otherParent.dreamling.DreamlingType);
        babyInstance.GetComponent<DreamlingCharacter>().SetBaby(baby);
        babyInstance.GetComponent<DreamlingCharacter>().BreedTimer.StartTimer();

        parentBarn.AddDreamling(baby);
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

    private void SetBaby(Dreamling newBorn)
    {
        dreamling = newBorn;
        //spriteRenderer ??= GetComponent<SpriteRenderer>();
        //spriteRenderer.sprite = Sprites[(int)dreamling.DreamlingType];
        for (int i = 0; i < Prefabs.Length; i++)
        {
            Prefabs[i].SetActive(i == (int)dreamling.DreamlingType);
        }
    }

    private DreamlingType GetDreamlingType(DreamlingType left, DreamlingType right)
    {
        return left == right ? left : Random.Range(0, 2) == 0 ? left : right;
    }
}
