using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI scoreText;
    public int score;

    void Update()
    {
        score = Mathf.FloorToInt(player.position.z);

        scoreText.text = "Score: " + score;

        Debug.Log("Score: " + score);
    }
}