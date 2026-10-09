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
    private readonly List<IPEndPoint> _clients = new List<IPEndPoint>();
    private int _maxPlayers = 4; // Quantidade de jogadores necessária para dar START

    private void Start()
    {
        _udpServer = new UdpClient(5001);
        _isRunning = true;
        
        _serverThread = new Thread(ReceiveMessages)
        {
            IsBackground = true
        };
        _serverThread.Start();
        
        Debug.Log("Servidor UDP rodando na porta 5001 (Aguardando conexões)...");
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

                //Trata conexões recebidas
                if (message.StartsWith("CONNECT"))
                {
                    // Se for o primeiro jogador, define o limite do servidor com base no modo
                    if (_clients.Count == 0)
                    {
                        if (message.Contains("1V1") || message.Contains("1v1"))
                        {
                            _maxPlayers = 2;
                            Debug.Log("Modo 1v1 configurado: Aguardando 2 jogadores...");
                        }
                        else
                        {
                            _maxPlayers = 4;
                            Debug.Log("Modo 2v2 configurado: Aguardando 4 jogadores...");
                        }
                    }

                    if (!_clients.Contains(clientEndPoint) && _clients.Count < _maxPlayers)
                    {
                        _clients.Add(clientEndPoint);
                        int playerId = _clients.Count;
                        SendMessageToClient($"ID:{playerId}", clientEndPoint);

                        Debug.Log($"Jogador {playerId}/{_maxPlayers} conectado.");

                        // Dispara o START assim que a meta de jogadores do modo atual for atingida
                        if (_clients.Count == _maxPlayers)
                        {
                            Debug.Log("Todos os jogadores conectados! Iniciando partida...");
                            BroadcastMessage("START");
                        }
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