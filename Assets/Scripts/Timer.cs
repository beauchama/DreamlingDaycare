using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 300;
    public bool timerIsRunning;
    public TextMeshProUGUI timerText;

    private void Start()
    {
        timerIsRunning = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!timerIsRunning)
        {
            return;
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            DisplayTime();
        }
        else
        {
            timeRemaining = 0;
            timerIsRunning = false;
            DisplayTime();

            GameManager.Instance.GameOver();
        }
    }

    public void DisplayTime()
    {
        float minutes = Mathf.FloorToInt(timeRemaining / 60);
        float seconds = Mathf.FloorToInt(timeRemaining % 60);

        timerText.text = $"{minutes:0}:{seconds:00}";
    }
}
