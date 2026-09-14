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
        // REMOVEMOS O AddStartingForce() DAQUI! A bola agora nasce parada.
    }
    
    public void StartNetworkGame()
    {
        ResetBall(); 
    }

    private void AddStartingForce()
    {
        // 1. Sorteia se o saque vai para a direita (0 graus) ou esquerda (180 graus)
        float baseAngle = Random.value < 0.5f ? 0f : 180f;
        
        // 2. Adiciona um desvio aleatório entre -45 e 45 graus para não ser um saque totalmente reto
        float offsetAngle = Random.Range(-45f, 45f);
        float finalAngle = baseAngle + offsetAngle;

        // 3. Converte o ângulo para radianos (necessário para a matemática da Unity)
        float radianAngle = finalAngle * Mathf.Deg2Rad;
        
        // 4. Calcula a direção X e Y usando seno e cosseno
        Vector2 direction = new Vector2(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle)).normalized;
        
        _rb.linearVelocity = direction * _currentSpeed; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Paddle paddle = collision.gameObject.GetComponent<Paddle>();
        
        if (paddle != null)
        {
            // Aumenta a velocidade até o limite máximo
            _currentSpeed = Mathf.Min(_currentSpeed * speedMultiplier, maxSpeed);
            
            // --- CÁLCULO DO ÂNGULO DA REBATIDA ---
            
            // 1. Pega as posições Y da bola e da raquete
            float ballPosY = transform.position.y;
            float paddlePosY = paddle.transform.position.y;
            
            // 2. Pega a altura total da raquete através do colisor
            float paddleHeight = collision.collider.bounds.size.y;
            
            // 3. Calcula o 'offset': um valor entre -1 (bateu na base) e 1 (bateu no topo)
            // Subtraímos a posição Y e dividimos pela metade da altura da raquete
            float offset = (ballPosY - paddlePosY) / (paddleHeight / 2f);
            
            // 4. Determina a direção no eixo X. Se a bola está na esquerda da tela (x < 0),
            // ela bateu na raquete esquerda e deve ir para a direita (1). Senão, vai para esquerda (-1).
            float directionX = transform.position.x < 0 ? 1f : -1f;
            
            // 5. Cria a nova direção. O X é fixo (direita ou esquerda) e o Y é o nosso offset.
            Vector2 newDirection = new Vector2(directionX, offset).normalized;
            
            // Aplica a nova velocidade e direção
            _rb.linearVelocity = newDirection * _currentSpeed;
        }
    }
    
    public void ResetBall()
    {
        transform.position = Vector2.zero; // Volta pro centro
        _rb.linearVelocity = Vector2.zero;
        _currentSpeed = initialSpeed;
        
        Invoke(nameof(AddStartingForce), 1f); 
    }
}