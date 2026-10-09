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
    public GameObject leftPaddleBack;   // P1 (1v1) / P2 (2v2 Defensor)
    public GameObject leftPaddleFront;  // P1 (2v2 Atacante)
    public GameObject rightPaddleFront; // P3 (2v2 Atacante)
    public GameObject rightPaddleBack;  // P2 (1v1) / P4 (2v2 Defensor) / IA

    private bool _isGameOver;

    private void Start()
    {
        if (winText != null) winText.gameObject.SetActive(false);

        bool isMultiplayer = (MenuManager.SelectedMode != GameMode.Singleplayer);
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);

        // 1. Controla a visibilidade das raquetes da frente (Ativas apenas no 2v2)
        if (leftPaddleFront != null) leftPaddleFront.SetActive(is2V2);
        if (rightPaddleFront != null) rightPaddleFront.SetActive(is2V2);

        // 2. MODO MULTIPLAYER (1v1 ou 2v2)
        if (isMultiplayer)
        {
            // Desativa a IA em TODAS as raquetes para não interferir nos comandos da rede
            DisableComputerPaddle(leftPaddleBack);
            DisableComputerPaddle(leftPaddleFront);
            DisableComputerPaddle(rightPaddleFront);
            DisableComputerPaddle(rightPaddleBack);

            // Ativa o cliente de rede
            if (udpClient != null) udpClient.enabled = true; 
        }
        // 3. MODO SOLO (vs Computador)
        else
        {
            if (leftPaddleBack != null)
            {
                var playerInput = leftPaddleBack.GetComponent<PlayerPaddle>();
                if (playerInput != null) playerInput.enabled = true;
            }

            if (rightPaddleBack != null)
            {
                var playerInput = rightPaddleBack.GetComponent<PlayerPaddle>();
                var computer = rightPaddleBack.GetComponent<ComputerPaddle>();

                if (playerInput != null) playerInput.enabled = false;
                if (computer != null) computer.enabled = true;
            }

            if (udpClient != null) udpClient.enabled = false;

            if (ball != null)
            {
                ball.enabled = true;
                ball.ResetBall();
            }
        }
    }

    private void DisableComputerPaddle(GameObject paddle)
    {
        if (paddle != null)
        {
            var computer = paddle.GetComponent<ComputerPaddle>();
            if (computer != null) computer.enabled = false;
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
            if (ball != null) ball.ResetBall();
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
            if (ball != null) ball.ResetBall();
        }

        if (MenuManager.SelectedMode != GameMode.Singleplayer && udpClient != null) 
            udpClient.SendScore(player1Score, player2Score);
    }

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