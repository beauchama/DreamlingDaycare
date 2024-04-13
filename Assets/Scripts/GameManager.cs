using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        if (Instance is not null)
        {
            Destroy(gameObject);
        }

        Instance = this;
    }
}
