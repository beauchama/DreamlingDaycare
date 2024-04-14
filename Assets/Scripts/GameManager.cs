using System.Collections.Generic;
using Daycare;
using Dreamlings.Characters;
using Dreamlings.Explorations;
using Dreamlings.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject Player;
    public Inventory Inventory = new Inventory();
    public GameOverManager gameOverManager;
    public Score score;
    public readonly List<Barn> Barns = new(3)
    {
        new Barn(),
        new Barn(),
        new Barn()
    };

    void Awake()
    {
        if (Instance is not null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Player = GameObject.FindGameObjectWithTag("Player");
        score = GetComponent<Score>();
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
            gameOverManager.DisplayGameOver(score.scoreText.text);
        }
    }

    private void ReloadCurrentScene()
    {
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void AddDreamlingToBarn(int barnIndex, Dreamling dreamling)
    {
        if (Barns.Count > barnIndex && barnIndex >= 0)
        {
            Barns[barnIndex].AddDreamling(dreamling);
        }
    }

    // Todo : remove the things below
    public void AddMeat()
    {
        Inventory.AddFood(NeededFood.Meat);
    }

    public void RemoveMeat()
    {
        Inventory.RemoveFood(NeededFood.Meat);
    }
}
