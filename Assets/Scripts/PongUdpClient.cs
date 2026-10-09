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
    public Transform leftPaddleFront;  // P1 (2v2 Atacante)
    public Transform leftPaddleBack;   // P2 (2v2 Defensor) / P1 (1v1)
    public Transform rightPaddleFront; // P3 (2v2 Atacante)
    public Transform rightPaddleBack;  // P4 (2v2 Defensor) / P2 (1v1)

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
    private bool _controlsConfigured;

    // Posições originais no eixo X salvas para o espelhamento da tela
    private float _startXLeftFront;
    private float _startXLeftBack;
    private float _startXRightFront;
    private float _startXRightBack;

    private readonly Dictionary<int, float> _playerTargetY = new Dictionary<int, float>();
    private Vector2 _networkBallPos;

    private bool _hasNewScoreUpdate;
    private int _latestP1Score;
    private int _latestP2Score;

    private void Start()
    {
        Application.runInBackground = true;

        if (MenuManager.SelectedMode == GameMode.Singleplayer)
        {
            this.enabled = false;
            return;
        }

        // Armazena as posições iniciais de X
        if (leftPaddleFront != null) _startXLeftFront = leftPaddleFront.position.x;
        if (leftPaddleBack != null) _startXLeftBack = leftPaddleBack.position.x;
        if (rightPaddleFront != null) _startXRightFront = rightPaddleFront.position.x;
        if (rightPaddleBack != null) _startXRightBack = rightPaddleBack.position.x;

        for (int i = 1; i <= 4; i++)
        {
            _playerTargetY[i] = 0f;
        }

        _udpClient = new UdpClient();
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(MenuManager.ServerIP), 5001);
        _isRunning = true;

        _clientThread = new Thread(ReceiveMessages)
        {
            IsBackground = true
        };
        _clientThread.Start();

        SendMessageToServer($"CONNECT:{MenuManager.SelectedMode}");

        if (ballScript != null) ballScript.enabled = false;
    }

    private bool IsTeamB()
    {
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);
        if (is2V2) return (_myId == 3 || _myId == 4);
        else return (_myId == 2);
    }

    private void Update()
    {
        // Configura controles locais e espelhamento visual assim que recebe o ID do servidor
        if (_myId != 0 && !_controlsConfigured)
        {
            _controlsConfigured = true;
            SetupLocalControls();
            ApplyInitialPositionsAndMirroring();
        }

        if (!_gameStarted) return;

        // Jogador 1 (Time A) inicia o movimento real da bola
        if (!_ballInitialized)
        {
            _ballInitialized = true;
            if (_myId == 1 && ballScript != null)
            {
                ballScript.enabled = true;
                ballScript.StartNetworkGame();
            }
        }

        // Atualiza o placar local
        if (_hasNewScoreUpdate)
        {
            _hasNewScoreUpdate = false;
            
            if (!IsTeamB())
                gameManager.UpdateScoreDirectly(_latestP1Score, _latestP2Score);
            else
                gameManager.UpdateScoreDirectly(_latestP2Score, _latestP1Score);
        }

        // Interpola a posição Y das outras raquetes
        UpdateOtherPaddles();

        // Envia a posição Y local para o servidor
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
            // Se for Time B, inverte a posição X da bola
            Vector2 targetBallPos = IsTeamB() ? new Vector2(-_networkBallPos.x, _networkBallPos.y) : _networkBallPos;
            ballTransform.position = Vector2.Lerp(ballTransform.position, targetBallPos, Time.deltaTime * 30f);
        }
    }

    private void SetupLocalControls()
    {
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);

        SetPlayerPaddleEnabled(leftPaddleFront, false);
        SetPlayerPaddleEnabled(leftPaddleBack, false);
        SetPlayerPaddleEnabled(rightPaddleFront, false);
        SetPlayerPaddleEnabled(rightPaddleBack, false);

        if (is2V2)
        {
            if (_myId == 1) SetPlayerPaddleEnabled(leftPaddleFront, true);
            else if (_myId == 2) SetPlayerPaddleEnabled(leftPaddleBack, true);
            else if (_myId == 3) SetPlayerPaddleEnabled(rightPaddleFront, true);
            else if (_myId == 4) SetPlayerPaddleEnabled(rightPaddleBack, true);
        }
        else // 1v1
        {
            if (_myId == 1) SetPlayerPaddleEnabled(leftPaddleBack, true);
            else if (_myId == 2) SetPlayerPaddleEnabled(rightPaddleBack, true);
        }
    }

    private void SetPlayerPaddleEnabled(Transform paddleTransform, bool enabledState)
    {
        if (paddleTransform != null)
        {
            var paddle = paddleTransform.GetComponent<PlayerPaddle>();
            if (paddle != null) paddle.enabled = enabledState;
        }
    }

    private void ApplyInitialPositionsAndMirroring()
    {
        float factor = IsTeamB() ? -1f : 1f;

        if (leftPaddleFront != null)
            leftPaddleFront.position = new Vector3(_startXLeftFront * factor, leftPaddleFront.position.y, 0);

        if (leftPaddleBack != null)
            leftPaddleBack.position = new Vector3(_startXLeftBack * factor, leftPaddleBack.position.y, 0);

        if (rightPaddleFront != null)
            rightPaddleFront.position = new Vector3(_startXRightFront * factor, rightPaddleFront.position.y, 0);

        if (rightPaddleBack != null)
            rightPaddleBack.position = new Vector3(_startXRightBack * factor, rightPaddleBack.position.y, 0);
    }

    private void SendMyPosition()
    {
        Transform myPaddle = GetMyPaddle();

        if (myPaddle != null)
        {
            string yPos = myPaddle.position.y.ToString(CultureInfo.InvariantCulture);
            SendMessageToServer($"POS:{_myId};{yPos}");
        }
    }

    private Transform GetMyPaddle()
    {
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);

        if (is2V2)
        {
            if (_myId == 1) return leftPaddleFront;
            if (_myId == 2) return leftPaddleBack;
            if (_myId == 3) return rightPaddleFront;
            if (_myId == 4) return rightPaddleBack;
        }
        else // 1v1
        {
            if (_myId == 1) return leftPaddleBack;
            if (_myId == 2) return rightPaddleBack;
        }
        return null;
    }

    private void UpdateOtherPaddles()
    {
        bool is2V2 = (MenuManager.SelectedMode == GameMode.Multiplayer2V2);

        if (is2V2)
        {
            if (_myId != 1 && leftPaddleFront != null) MovePaddle(leftPaddleFront, _playerTargetY[1]);
            if (_myId != 2 && leftPaddleBack != null) MovePaddle(leftPaddleBack, _playerTargetY[2]);
            if (_myId != 3 && rightPaddleFront != null) MovePaddle(rightPaddleFront, _playerTargetY[3]);
            if (_myId != 4 && rightPaddleBack != null) MovePaddle(rightPaddleBack, _playerTargetY[4]);
        }
        else // 1v1
        {
            if (_myId == 1 && rightPaddleBack != null) MovePaddle(rightPaddleBack, _playerTargetY[2]);
            if (_myId == 2 && leftPaddleBack != null) MovePaddle(leftPaddleBack, _playerTargetY[1]);
        }
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