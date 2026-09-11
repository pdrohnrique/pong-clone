using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    public bool isPlayer1Goal; // Marque TRUE no gol da esquerda, deixe FALSE no da direita
    public GameManager gameManager; // Arraste o GameManager aqui

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se o objeto que bater tiver a tag "Ball" (Lembre-se de colocar a tag "Ball" na sua bola!)
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (isPlayer1Goal)
                gameManager.ComputerScored(); // Bola bateu na esquerda, ponto da IA
            else
                gameManager.PlayerScored(); // Bola bateu na direita, ponto do Player
        }
    }
}