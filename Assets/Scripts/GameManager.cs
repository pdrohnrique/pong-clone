using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Placar e Regras")]
    public int player1Score;
    public int player2Score;
    public int maxScore = 5;

    [Header("Elementos de UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI winText;

    [Header("Referências da Cena")]
    public Ball ball;
    public GameObject rightPaddle;
    public PongUdpClient udpClient; // Apenas o Cliente de rede fica aqui!

    private bool _isGameOver;

    private void Start()
    {
        if (winText != null) winText.gameObject.SetActive(false);

        // --- MODO MULTIPLAYER ---
        if (MenuManager.IsMultiplayerMode)
        {
            if (rightPaddle != null)
            {
                var computer = rightPaddle.GetComponent<ComputerPaddle>();
                if (computer != null) computer.enabled = false; // Desativa IA
            }

            // Ativa o cliente de rede que vai buscar o servidor externo
            if (udpClient != null) udpClient.enabled = true; 
        }
        // --- MODO SOLO (vs Computador) ---
        else
        {
            if (rightPaddle != null)
            {
                var playerInput = rightPaddle.GetComponent<PlayerPaddle>();
                var computer = rightPaddle.GetComponent<ComputerPaddle>();

                if (playerInput != null) playerInput.enabled = false; // Desativa teclado na direita
                if (computer != null) computer.enabled = true;       // Ativa IA na direita
            }

            // Desativa o cliente UDP no modo offline
            if (udpClient != null) udpClient.enabled = false; 

            // No modo Solo, solta a bola imediatamente sem esperar conexões
            if (ball != null)
            {
                ball.enabled = true;
                ball.ResetBall();
            }
        }
    }

    public void Player1Scored()
    {
        if (_isGameOver) return;

        player1Score++;
        UpdateUI();

        if (player1Score >= maxScore)
        {
            EndGame("Você ganhou!");
        }
        else
        {
            ball.ResetBall();
        }

        if (MenuManager.IsMultiplayerMode && udpClient != null) 
            udpClient.SendScore(player1Score, player2Score);
    }

    public void Player2Scored()
    {
        if (_isGameOver) return;

        player2Score++;
        UpdateUI();

        if (player2Score >= maxScore)
        {
            EndGame("Você perdeu...");
        }
        else
        {
            ball.ResetBall();
        }

        if (MenuManager.IsMultiplayerMode && udpClient != null) 
            udpClient.SendScore(player1Score, player2Score);
    }

// Método chamado via rede para o Player 2
    public void UpdateScoreDirectly(int p1, int p2)
    {
        player1Score = p1;
        player2Score = p2;
        UpdateUI();

        if (player1Score >= maxScore) 
            EndGame("Você ganhou!");
        else if (player2Score >= maxScore) 
            EndGame("Você perdeu...");
    }

    private void EndGame(string winnerMessage)
    {
        _isGameOver = true;

        if (ball != null) ball.gameObject.SetActive(false);

        if (winText != null)
        {
            winText.text = winnerMessage;
            winText.gameObject.SetActive(true);
        }

        Invoke(nameof(ReturnToMenu), 3f);
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateUI()
    {
        if (player1ScoreText != null) player1ScoreText.text = player1Score.ToString();
        if (player2ScoreText != null) player2ScoreText.text = player2Score.ToString();
    }
}