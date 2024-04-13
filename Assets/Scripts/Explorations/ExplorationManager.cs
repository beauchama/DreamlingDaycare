using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationManager : MonoBehaviour
{
    public void EndExploration()
    {
        SceneManager.LoadScene("Daycare");
    }
}
