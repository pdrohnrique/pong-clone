using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

public class PongUdpClient : MonoBehaviour
{
    public Transform leftPaddle;
    public Transform rightPaddle;
    public Transform ballTransform;
    public Ball ballScript;
    public GameManager gameManager;

    private UdpClient _udpClient;
    private IPEndPoint _serverEndPoint;
    private Thread _clientThread;
    private bool _isRunning;

    private int _myId;
    private bool _gameStarted;
    private bool _ballInitialized; // Garante que a bola só receba o 1º chute uma vez
    
    private Transform _myPaddle;
    private Transform _opponentPaddle;
    private float _opponentTargetY;
    private Vector2 _networkBallPos;

    private bool _hasNewScoreUpdate;
    private int _latestP1Score;
    private int _latestP2Score;

    private void Start()
    {
        Application.runInBackground = true;

        _udpClient = new UdpClient();
    
        // Lê o IP definido pelo usuário no MenuManager
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
    // AMBOS os jogadores jogam com a raquete da ESQUERDA na própria tela!
    if (_myId != 0 && _myPaddle == null)
    {
        _myPaddle = leftPaddle;         // Minha raquete na minha tela SEMPRE é a esquerda
        _opponentPaddle = rightPaddle; // Oponente na minha tela SEMPRE é a direita

        // Ativa o teclado apenas na raquete da esquerda (sua raquete)
        if (leftPaddle.GetComponent<PlayerPaddle>() != null)
            leftPaddle.GetComponent<PlayerPaddle>().enabled = true;

        if (rightPaddle.GetComponent<PlayerPaddle>() != null)
            rightPaddle.GetComponent<PlayerPaddle>().enabled = false;
    }

    if (!_gameStarted) return;

    if (!_ballInitialized)
    {
        _ballInitialized = true;
        if (_myId == 1 && ballScript != null)
        {
            ballScript.enabled = true;
            ballScript.StartNetworkGame(); 
        }
    }

    // Atualiza o placar local mantendo SEU ponto na esquerda
    if (_hasNewScoreUpdate)
    {
        _hasNewScoreUpdate = false;
        
        if (_myId == 1)
            gameManager.UpdateScoreDirectly(_latestP1Score, _latestP2Score);
        else
            gameManager.UpdateScoreDirectly(_latestP2Score, _latestP1Score); // Inverte UI pro P2
    }

    // Move a raquete do oponente (recebe o Y direto do outro player)
    if (_opponentPaddle != null)
    {
        Vector2 targetPos = new Vector2(_opponentPaddle.position.x, _opponentTargetY);
        _opponentPaddle.position = Vector3.Lerp(_opponentPaddle.position, targetPos, Time.deltaTime * 20f);
    }

    // Envia o movimento Y do seu teclado para a rede
    string yPos = _myPaddle.position.y.ToString(CultureInfo.InvariantCulture);
    SendMessageToServer($"POS:{_myId};{yPos}");

    // JOGADOR 1: Envia a bola normal
    if (_myId == 1 && ballTransform != null)
    {
        string ballX = ballTransform.position.x.ToString(CultureInfo.InvariantCulture);
        string ballY = ballTransform.position.y.ToString(CultureInfo.InvariantCulture);
        SendMessageToServer($"BALL:{ballX};{ballY}");
    }
    // JOGADOR 2: Aplica a posição da bola INVERTIDA no eixo X
    else if (_myId == 2 && ballTransform != null)
    {
        ballTransform.position = Vector2.Lerp(ballTransform.position, _networkBallPos, Time.deltaTime * 30f);
    }
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
                            _opponentTargetY = parsedY;
                    }
                }
                else if (message.StartsWith("BALL:") && _myId == 2)
                {
                    string body = message.Substring(5);
                    string[] parts = body.Split(';');
                    if (float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float bx) &&
                        float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float by))
                    {
                        // INVERSÃO DA BOLA: Multiplica o X por -1
                        _networkBallPos = new Vector2(-bx, by);
                    }
                }
                else if (message.StartsWith("SCORE:") && _myId == 2)
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