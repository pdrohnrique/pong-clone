using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    public bool isPlayer1Goal; // TRUE = Gol da Esquerda, FALSE = Gol da Direita
    public GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (isPlayer1Goal)
                gameManager.Player2Scored(); // Bola bateu no gol da esquerda -> ponto do Player 2
            else
                gameManager.Player1Scored(); // Bola bateu no gol da direita -> ponto do Player 1
        }
    }
}