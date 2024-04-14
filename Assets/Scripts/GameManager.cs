using System.Collections.Generic;
using System.Linq;
using Daycare;
using Dreamlings.Characters;
using Dreamlings.Explorations;
using Dreamlings.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject Player;
    public GameObject barnResidentsDisplay;
    public Inventory Inventory = new Inventory();
    public GameOverManager gameOverManager;
    public Score score;
    public int currentBarnIndex = -1;

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

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DisplayBarnResidents();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            HideBarnResidents();
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

    public void AddDreamlingToBarn(Dreamling dreamling)
    {
        if (Barns.Count > currentBarnIndex && currentBarnIndex >= 0)
        {
            Barns[currentBarnIndex].AddDreamling(dreamling);
        }
    }

    private void DisplayBarnResidents()
    {
        UpdateBarnNames();
        if (barnResidentsDisplay is null)
        {
            return;
        }

        barnResidentsDisplay.SetActive(true);
    }

    private void HideBarnResidents()
    {
        if (barnResidentsDisplay is null)
        {
            return;
        }

        barnResidentsDisplay.SetActive(false);
    }

    private void UpdateBarnNames()
    {
        if (barnResidentsDisplay is null)
        {
            return;
        }

        var textBoxes = barnResidentsDisplay.GetComponentsInChildren<TextMeshProUGUI>();

        for (var barnIndex = 0; barnIndex < Barns.Count; barnIndex++)
        {
            var barn = Barns[barnIndex];
            for (var dIndex = 0; dIndex < 6; dIndex++)
            {
                var dreamling = barn.Dreamlings.Count > dIndex ? barn.Dreamlings[dIndex] : null;
                var dreamlingDame = dreamling?.Name ?? string.Empty;

                var label = $"B{barnIndex + 1}D{dIndex + 1}";
                var textObj = textBoxes.SingleOrDefault(x => x.name == label);
                if (textObj is not null)
                {
                    textObj.text = dreamlingDame;
                }
            }
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
