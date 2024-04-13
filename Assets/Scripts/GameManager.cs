using Dreamlings.Explorations;
using Dreamlings.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject Player;
    public float TimeRemaining;
    public Inventory Inventory = new Inventory();
    public GameOverManager gameOverManager;

    void Awake()
    {
        if (Instance is not null)
        {
            Destroy(gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadCurrentScene();
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        if (gameOverManager is not null)
        {
            gameOverManager.DisplayGameOver(2461);
        }
    }

    private void ReloadCurrentScene()
    {
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void AddMeat()
    {
        Inventory.AddFood(NeededFood.Meat);
    }

    public void RemoveMeat()
    {
        Inventory.RemoveFood(NeededFood.Meat);
    }
}
