using UnityEngine;
using UnityEngine.SceneManagement;

public class FixedCameras : MonoBehaviour
{
    float newAspectRatio = 16.0f / 9.0f;
    private Vector2 lastScreenSize;

    private void Awake()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        //FixRatio();
        SceneManager.sceneLoaded += (x, y) => FixRatio();
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        if (lastScreenSize != screenSize)
        {
            lastScreenSize = screenSize;                     //  Launch the event when the screen size change
            FixRatio();
        }
    }

    private void FixRatio()
    {
        var variance = newAspectRatio / Camera.main.aspect;
        if (variance < 1.0f)
            Camera.main.rect = new Rect((1.0f - variance) / 2.0f, 0f, variance, 1.0f);
        else
        {
            variance = 1.0f / variance;
            Camera.main.rect = new Rect(0, (1.0f - variance) / 2.0f, 1.0f, variance);
        }
    }
}
