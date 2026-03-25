using UnityEngine;

public class ComputerPaddle : Paddle
{
    public Rigidbody2D ball;
    
    [SerializeField] private float deadZone = 0.15f;
    [SerializeField] private float acceleration = 20f;
    
    private float _targetDirectionY;
    
    private void Update()
    {
        float targetY = ball.linearVelocity.x > 0f ? ball.position.y : 0f;
        float delta = targetY - transform.position.y;

        if (Mathf.Abs(delta) <= deadZone)
        {
            _targetDirectionY = 0f;
        }
        else
        {
            _targetDirectionY = Mathf.Sign(delta);
        }
    }
    
    private void FixedUpdate()
    {
        float targetVelocityY = _targetDirectionY * speed;
        float newVelocityY = Mathf.MoveTowards(Rb.linearVelocity.y, targetVelocityY, acceleration * Time.fixedDeltaTime);

        Rb.linearVelocity = new Vector2(0f, newVelocityY);
    }
}
