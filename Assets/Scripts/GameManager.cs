using System.Collections.Generic;
using System.Linq;
using Daycare;
using Dreamlings.Characters;
using Dreamlings.Explorations;
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
    public ErrorMessage errorMessageDisplay;
    public int currentBarnIndex = -1;
    public int LastSceneIndex = -1;
    public bool gameIsOver;

    public readonly List<Barn> Barns = new(3)
    {
        new Barn(),
        new Barn(),
        new Barn()
    };

    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Player = GameObject.FindGameObjectWithTag("Player");
        score = GetComponent<Score>();
        gameIsOver = false;
    }

    private void Update()
    {
        if (gameIsOver)
            return;

        // Dev only, you don't get to use that, haha
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     ReloadCurrentScene();
        // }

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
            Player.SetActive(false);
            gameIsOver = true;
            gameOverManager.DisplayGameOver(score.scoreText.text);
        }
    }

    public void ReloadCurrentScene()
    {
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public bool AddDreamlingToBarn(Dreamling dreamling)
    {
        // If it's not a valid barn index, don't add the dreamling.
        if (Barns.Count <= currentBarnIndex || currentBarnIndex < 0)
            return true;

        var errorMessage = Barns[currentBarnIndex].AddDreamling(dreamling);

        if (errorMessage is not null)
        {
            errorMessageDisplay?.DisplayError(errorMessage);
            return false;
        }

        return true;
    }

    public void RemoveDreamlingFromBarn(Dreamling dreamling)
    {
        // Find which barn the dreamling is in
        var barnIndex = Barns.FindIndex(barn => barn.Dreamlings.Contains(dreamling));
        if (barnIndex >= 0)
        {
            Barns[barnIndex].RemoveDreamling(dreamling);
        }
    }

    public Barn GetDreamlingBarn(Dreamling dreamling)
    {
        return Barns.FirstOrDefault(barn => barn.HasDreamling(dreamling));
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

    public void ResetGame()
    {
        Destroy(Instance.gameObject);
        Instance = null;
        PlayerManager.Instance = null;
    }
}
