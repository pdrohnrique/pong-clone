using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

public class PongUdpClient : MonoBehaviour
{
    [Header("Raquetes na Cena")]
    public Transform leftPaddleFront;  // P1: Time A - Atacante
    public Transform leftPaddleBack;   // P2: Time A - Defensor
    public Transform rightPaddleFront; // P3: Time B - Atacante
    public Transform rightPaddleBack;  // P4: Time B - Defensor

    public Transform ballTransform;
    public Ball ballScript;
    public GameManager gameManager;

    private UdpClient _udpClient;
    private IPEndPoint _serverEndPoint;
    private Thread _clientThread;
    private bool _isRunning;

    private int _myId;
    private bool _gameStarted;
    private bool _ballInitialized; 
    
    // Dicionário para armazenar as posições Y recebidas pela rede para cada ID
    private Dictionary<int, float> _playerTargetY = new Dictionary<int, float>();
    private Vector2 _networkBallPos;

    private bool _hasNewScoreUpdate;
    private int _latestP1Score;
    private int _latestP2Score;

    private void Start()
    {
        Application.runInBackground = true;

        // Se estiver no modo Singleplayer, cancela a inicialização de rede
        if (MenuManager.SelectedMode == GameMode.Singleplayer)
        {
            this.enabled = false;
            return;
        }

        // Inicializa o dicionário de posições de rede para até 4 jogadores
        for (int i = 1; i <= 4; i++)
        {
            _playerTargetY[i] = 0f;
        }

        _udpClient = new UdpClient();
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(MenuManager.ServerIP), 5001);
        _isRunning = true;

        _clientThread = new Thread(ReceiveMessages);
        _clientThread.IsBackground = true;
        _clientThread.Start();

        SendMessageToServer("CONNECT");

        if (ballScript != null) ballScript.enabled = false;
    }

    private void Update()
    {
        // Define qual raquete reage ao seu teclado no início da partida
        if (_myId != 0 && !_ballInitialized)
        {
            SetupLocalControls();
        }

        if (!_gameStarted) return;

        // Jogador 1 inicia o movimento real da bola
        if (!_ballInitialized)
        {
            _ballInitialized = true;
            if (_myId == 1 && ballScript != null)
            {
                ballScript.enabled = true;
                ballScript.StartNetworkGame(); 
            }
        }

        // Atualiza o placar local (Time A = IDs 1 e 2 | Time B = IDs 3 e 4)
        if (_hasNewScoreUpdate)
        {
            _hasNewScoreUpdate = false;
            
            if (_myId <= 2) // Time A
                gameManager.UpdateScoreDirectly(_latestP1Score, _latestP2Score);
            else // Time B (Inverte a UI)
                gameManager.UpdateScoreDirectly(_latestP2Score, _latestP1Score); 
        }

        // Interpola a posição de todos os outros 3 jogadores
        UpdateOtherPaddles();

        // Envia sua posição Y atual pela rede
        SendMyPosition();

        // BOLA: Jogador 1 envia, os demais recebem
        if (_myId == 1 && ballTransform != null)
        {
            string ballX = ballTransform.position.x.ToString(CultureInfo.InvariantCulture);
            string ballY = ballTransform.position.y.ToString(CultureInfo.InvariantCulture);
            SendMessageToServer($"BALL:{ballX};{ballY}");
        }
        else if (_myId > 1 && ballTransform != null)
        {
            // IDs 3 e 4 (Time B) veem o eixo X da bola invertido
            Vector2 targetBallPos = (_myId > 2) ? new Vector2(-_networkBallPos.x, _networkBallPos.y) : _networkBallPos;
            ballTransform.position = Vector2.Lerp(ballTransform.position, targetBallPos, Time.deltaTime * 30f);
        }
    }

    private void SetupLocalControls()
    {
        // Habilita o script de controle (PlayerPaddle) apenas para o seu respectivo ID
        if (leftPaddleFront != null) leftPaddleFront.GetComponent<PlayerPaddle>().enabled = (_myId == 1);
        if (leftPaddleBack != null) leftPaddleBack.GetComponent<PlayerPaddle>().enabled = (_myId == 2);
        if (rightPaddleFront != null) rightPaddleFront.GetComponent<PlayerPaddle>().enabled = (_myId == 3);
        if (rightPaddleBack != null) rightPaddleBack.GetComponent<PlayerPaddle>().enabled = (_myId == 4);
    }

    private void SendMyPosition()
    {
        Transform myPaddle = null;
        if (_myId == 1) myPaddle = leftPaddleFront;
        else if (_myId == 2) myPaddle = leftPaddleBack;
        else if (_myId == 3) myPaddle = rightPaddleFront;
        else if (_myId == 4) myPaddle = rightPaddleBack;

        if (myPaddle != null)
        {
            string yPos = myPaddle.position.y.ToString(CultureInfo.InvariantCulture);
            SendMessageToServer($"POS:{_myId};{yPos}");
        }
    }

    private void UpdateOtherPaddles()
    {
        if (_myId != 1 && leftPaddleFront != null) MovePaddle(leftPaddleFront, _playerTargetY[1]);
        if (_myId != 2 && leftPaddleBack != null) MovePaddle(leftPaddleBack, _playerTargetY[2]);
        if (_myId != 3 && rightPaddleFront != null) MovePaddle(rightPaddleFront, _playerTargetY[3]);
        if (_myId != 4 && rightPaddleBack != null) MovePaddle(rightPaddleBack, _playerTargetY[4]);
    }

    private void MovePaddle(Transform paddle, float targetY)
    {
        Vector2 targetPos = new Vector2(paddle.position.x, targetY);
        paddle.position = Vector3.Lerp(paddle.position, targetPos, Time.deltaTime * 20f);
    }

    private void ReceiveMessages()
    {
        while (_isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = _udpClient.Receive(ref anyIP);
                string message = Encoding.UTF8.GetString(receivedBytes);

                if (message.StartsWith("ID:"))
                {
                    _myId = int.Parse(message.Split(':')[1]);
                }
                else if (message == "START")
                {
                    _gameStarted = true;
                }
                else if (message.StartsWith("POS:"))
                {
                    string[] parts = message.Split(';');
                    int senderId = int.Parse(parts[0].Split(':')[1]);
                    if (senderId != _myId)
                    {
                        if (float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedY))
                        {
                            _playerTargetY[senderId] = parsedY;
                        }
                    }
                }
                else if (message.StartsWith("BALL:") && _myId > 1)
                {
                    string body = message.Substring(5);
                    string[] parts = body.Split(';');
                    if (float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float bx) &&
                        float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float by))
                    {
                        _networkBallPos = new Vector2(bx, by);
                    }
                }
                else if (message.StartsWith("SCORE:") && _myId > 1)
                {
                    string body = message.Substring(6);
                    string[] parts = body.Split(';');
                    _latestP1Score = int.Parse(parts[0]);
                    _latestP2Score = int.Parse(parts[1]);
                    _hasNewScoreUpdate = true;
                }
            }
            catch (SocketException) { }
            catch (System.Exception e) { if (_isRunning) Debug.LogError("Erro Cliente: " + e.Message); }
        }
    }

    private void SendMessageToServer(string message)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        _udpClient.Send(sendBytes, sendBytes.Length, _serverEndPoint);
    }

    public void SendScore(int p1, int p2)
    {
        if (_myId == 1)
        {
            SendMessageToServer($"SCORE:{p1};{p2}");
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
        _udpClient?.Close();
        _clientThread?.Abort();
    }
}