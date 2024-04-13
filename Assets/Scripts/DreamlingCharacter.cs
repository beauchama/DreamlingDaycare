using Dreamlings.Characters;
using UnityEngine;

public class DreamlingCharacter : MonoBehaviour
{
    private Dreamling dreamling;
    private float timer;
    private Vector3 targetPosition;

    public float moveSpeed = 1f;
    public float changeInterval = 2f;
    public Sprite[] Sprites;

    // Start is called before the first frame update
    void Start()
    {
        dreamling = new Dreamling();

        GetComponent<SpriteRenderer>().sprite = Sprites[Random.Range(0, Sprites.Length)];

        timer = changeInterval;
        SetRandomTargetPosition();
    }

    // Update is called once per frame
    void Update()
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

    void SetRandomTargetPosition()
    {
        targetPosition = transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }
}
