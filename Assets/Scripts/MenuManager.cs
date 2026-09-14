using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static bool IsMultiplayerMode;
    public static string ServerIP = "127.0.0.1"; // IP padrão para testes no mesmo PC

    public TMP_InputField ipInputField; // Arraste o InputField aqui no Inspector

    public void PlaySolo()
    {
        IsMultiplayerMode = false;
        SceneManager.LoadScene("GameScene");
    }

    public void PlayMultiplayer()
    {
        IsMultiplayerMode = true;

        // Se o jogador digitou um IP, utiliza ele; caso contrário, usa 127.0.0.1
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            ServerIP = ipInputField.text.Trim();
        }
        else
        {
            ServerIP = "127.0.0.1";
        }

        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}