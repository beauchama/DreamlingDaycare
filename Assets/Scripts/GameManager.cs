using Dreamlings.Explorations;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public float TimeRemaining;
    public Inventory Inventory = new Inventory();

    void Awake()
    {
        if (Instance is not null)
        {
            Destroy(gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
    }
}
