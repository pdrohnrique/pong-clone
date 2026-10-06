using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Enum que define os 3 modos de jogo possíveis
public enum GameMode
{
    Singleplayer,   // VS Computador (1 Raquete)
    Multiplayer1V1, // Online 1v1 (2 Raquetes)
    Multiplayer2V2  // Online 2v2 (4 Raquetes)
}

public class MenuManager : MonoBehaviour
{
    public static GameMode SelectedMode = GameMode.Singleplayer;
    public static string ServerIP = "127.0.0.1"; // IP padrão para testes locais

    public TMP_InputField ipInputField; // Arraste o InputField aqui no Inspector

    // Auxiliar para capturar o IP digitado
    private void ReadIP()
    {
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            ServerIP = ipInputField.text.Trim();
        }
        else
        {
            ServerIP = "127.0.0.1";
        }
    }

    // Botão: "Computador" (Solo)
    public void PlaySolo()
    {
        SelectedMode = GameMode.Singleplayer;
        SceneManager.LoadScene("GameScene");
    }

    // Botão: "Multiplayer 1v1"
    public void PlayMultiplayer1V1()
    {
        ReadIP();
        SelectedMode = GameMode.Multiplayer1V1;
        SceneManager.LoadScene("GameScene");
    }

    // Botão: "Multiplayer 2v2"
    public void PlayMultiplayer2V2()
    {
        ReadIP();
        SelectedMode = GameMode.Multiplayer2V2;
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}