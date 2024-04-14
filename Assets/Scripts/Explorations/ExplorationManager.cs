using Dreamlings.Explorations;
using Dreamlings.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;
    private Inventory Inventory = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance.Player.transform.position.y < -10)
        {
            ResetLevel();
        }
    }

    public void AddFood(NeededFood neededFood)
    {
        Inventory.AddFood(neededFood, true);
    }

    public void ResetLevel()
    {
        GameManager.Instance.Inventory.RemoveInventory(Inventory);
        GameManager.Instance.ReloadCurrentScene();
    }

    public void EndExploration()
    {
        SceneTransitionManager.Instance.ChangeScene("Daycare Dom");
    }
}
