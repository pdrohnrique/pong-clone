using UnityEngine;

public class PlayerPaddle : Paddle
{
    private Vector2 _direction;

    private void Update()
    {
        float input = Input.GetAxisRaw("Vertical");
        _direction = new Vector2(0f, input);
    }

    private void FixedUpdate()
    { 
        Rb.linearVelocity = new Vector2(0f, _direction.y * speed);
    }
}
