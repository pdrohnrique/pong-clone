using UnityEngine;

public class Ball : MonoBehaviour
{
    public float initialSpeed = 10f;
    public float speedMultiplier = 1.1f; // Aumenta 10% a cada rebatida
    public float maxSpeed = 25f;

    private float _currentSpeed;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    
    private void Start()
    {
        _currentSpeed = initialSpeed;
        AddStartingForce();
    }

    private void AddStartingForce()
    {
        float x = Random.value < 0.5f ? -1f : 1f;
        float y = Random.value < 0.5f ? Random.Range(-1f, -0.5f) : Random.Range(0.5f, 1f);
        
        Vector2 direction = new Vector2(x, y).normalized;
        
        // Em vez de AddForce, setar a velocidade diretamente dá um controle mais arcade
        _rb.linearVelocity = direction * _currentSpeed; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se bateu em algo que tem o script Paddle (seja Player ou Computer)
        if (collision.gameObject.GetComponent<Paddle>() != null)
        {
            // Aumenta a velocidade até o limite máximo
            _currentSpeed = Mathf.Min(_currentSpeed * speedMultiplier, maxSpeed);
            
            // Mantém a direção atual, mas aplica a nova velocidade
            _rb.linearVelocity = _rb.linearVelocity.normalized * _currentSpeed;
        }
    }
}