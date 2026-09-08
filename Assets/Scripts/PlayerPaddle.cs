using UnityEngine;
using UnityEngine.InputSystem; // Necessário para o novo Input System

public class PlayerPaddle : Paddle
{
    private Vector2 _direction;

    private void Update()
    {
        float moveY = 0f;

        // Verifica se o teclado está conectado para evitar erros
        if (Keyboard.current != null)
        {
            // Lê W/S ou Setas Cima/Baixo
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                moveY = 1f;
            else if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                moveY = -1f;
        }

        _direction = new Vector2(0f, moveY);
    }

    private void FixedUpdate()
    { 
        // Na Unity 6+ usamos linearVelocity, em versões anteriores use velocity
        Rb.linearVelocity = new Vector2(0f, _direction.y * speed);
    }
}