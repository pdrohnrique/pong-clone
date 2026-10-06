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
    public PongUdpClient udpClient;

    [Header("Raquetes na Cena")]
    public GameObject leftPaddleBack;   // P1 / Jogador Principal (Esquerda)
    public GameObject leftPaddleFront;  // P2 (Time A - Apenas no 2v2)
    public GameObject rightPaddleFront; // P3 (Time B - Apenas no 2v2)
    public GameObject rightPaddleBack;  // P4 / IA (Direita)

    private bool _isGameOver;

    private void Start()
    {
        if (winText != null) winText.gameObject.SetActive(false);

        bool isMultiplayer = (MenuManager.SelectedMode != GameMode.Singleplayer);
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);

        // 1. Controla a visibilidade das raquetes avançadas da frente (Ativas apenas no 2v2)
        if (leftPaddleFront != null) leftPaddleFront.SetActive(is2V2);
        if (rightPaddleFront != null) rightPaddleFront.SetActive(is2V2);

        // 2. MODO MULTIPLAYER (1v1 ou 2v2)
        if (isMultiplayer)
        {
            // Desativa a IA da raquete direita
            if (rightPaddleBack != null)
            {
                var computer = rightPaddleBack.GetComponent<ComputerPaddle>();
                if (computer != null) computer.enabled = false;
            }

            // Ativa o cliente de rede
            if (udpClient != null) udpClient.enabled = true; 
        }
        // 3. MODO SOLO (vs Computador)
        else
        {
            if (leftPaddleBack != null)
            {
                var playerInput = leftPaddleBack.GetComponent<PlayerPaddle>();
                if (playerInput != null) playerInput.enabled = true; // Jogador controla esquerda
            }

            if (rightPaddleBack != null)
            {
                var playerInput = rightPaddleBack.GetComponent<PlayerPaddle>();
                var computer = rightPaddleBack.GetComponent<ComputerPaddle>();

                if (playerInput != null) playerInput.enabled = false; // Desativa teclado na direita
                if (computer != null) computer.enabled = true;        // Ativa IA na direita
            }

            // Desativa o cliente de rede no modo offline
            if (udpClient != null) udpClient.enabled = false; 

            // Solta a bola imediatamente sem esperar conexões de rede
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

        if (MenuManager.SelectedMode != GameMode.Singleplayer && udpClient != null) 
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

        if (MenuManager.SelectedMode != GameMode.Singleplayer && udpClient != null) 
            udpClient.SendScore(player1Score, player2Score);
    }

    // Método chamado via rede para sincronização de placar
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