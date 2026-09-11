using UnityEngine;
using TMPro; // Biblioteca para os textos da UI

public class GameManager : MonoBehaviour
{
    public int playerScore;
    public int computerScore;

    public TextMeshProUGUI playerScoreText; // Arraste o Score_Player1
    public TextMeshProUGUI computerScoreText; // Arraste o Score_Player2

    public Ball ball; // Arraste a Bola

    public void PlayerScored()
    {
        playerScore++;
        playerScoreText.text = playerScore.ToString();
        ball.ResetBall();
    }

    public void ComputerScored()
    {
        computerScore++;
        computerScoreText.text = computerScore.ToString();
        ball.ResetBall();
    }
}