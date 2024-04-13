using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    private int score;

    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = score.ToString();
    }
}