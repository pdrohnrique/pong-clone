using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class PongUdpServer : MonoBehaviour
{
    private UdpClient _udpServer;
    private Thread _serverThread;
    private bool _isRunning;
    private List<IPEndPoint> _clients = new List<IPEndPoint>();

    private void Start()
    {
        _udpServer = new UdpClient(5001);
        _isRunning = true;
        
        _serverThread = new Thread(ReceiveMessages);
        _serverThread.IsBackground = true;
        _serverThread.Start();
        
        Debug.Log("Servidor UDP 2v2 rodando na porta 5001 (Aguardando 4 jogadores)...");
    }

    private void ReceiveMessages()
    {
        while (_isRunning)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = _udpServer.Receive(ref clientEndPoint);
                string message = Encoding.UTF8.GetString(receivedBytes);

                // Agora aguarda até 4 jogadores
                if (!_clients.Contains(clientEndPoint) && _clients.Count < 4)
                {
                    _clients.Add(clientEndPoint);
                    int playerId = _clients.Count;
                    SendMessageToClient($"ID:{playerId}", clientEndPoint);

                    // Quando o 4º jogador conecta, avisa TODOS para começar o jogo
                    if (_clients.Count == 4)
                    {
                        BroadcastMessage("START");
                    }
                }

                // Repassa mensagens de POS (raquete), BALL (bola) e SCORE (placar)
                if (message.StartsWith("POS:") || message.StartsWith("BALL:") || message.StartsWith("SCORE:"))
                {
                    foreach (var client in _clients)
                    {
                        if (!client.Equals(clientEndPoint))
                        {
                            SendMessageToClient(message, client);
                        }
                    }
                }
            }
            catch (SocketException) { }
            catch (System.Exception e) { if (_isRunning) Debug.LogError("Erro Servidor: " + e.Message); }
        }
    }

    private void SendMessageToClient(string message, IPEndPoint endPoint)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        _udpServer.Send(sendBytes, sendBytes.Length, endPoint);
    }

    private new void BroadcastMessage(string message)
    {
        foreach (var client in _clients)
        {
            SendMessageToClient(message, client);
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
        _udpServer?.Close();
        _serverThread?.Abort();
    }
}