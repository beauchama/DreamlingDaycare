using TMPro;
using UnityEngine;

public class BreedTimer : MonoBehaviour
{
    public bool IsRunning = false;
    public float timeRemaining = 10;
    public TextMeshProUGUI timerText;

    private void Update()
    {
        if (!IsRunning)
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
            IsRunning = false;
            timerText.gameObject.SetActive(false);
            timerText.text = string.Empty;
        }
    }

    public void StartTimer()
    {
        IsRunning = true;
        timeRemaining = 10;
        timerText.gameObject.SetActive(true);
    }

    public void DisplayTime()
    {
        float seconds = Mathf.FloorToInt(timeRemaining % 60);

        timerText.text = $"Can breed: {seconds:00}";
    }
}