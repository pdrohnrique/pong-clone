using UnityEngine;

public class ComputerPaddle : Paddle
{
    public Transform ball; // Arraste a Bola aqui no Inspector

    private void FixedUpdate()
    {
        if (ball == null) return;

        // 1. O alvo da IA é a sua própria posição X, mas buscando o Y exato da bolinha
        Vector2 targetPosition = new Vector2(transform.position.x, ball.position.y);
        
        // 2. MoveTowards avança até o alvo, mas matematicamente NUNCA passa dele. 
        // O tamanho do passo é a sua 'speed' multiplicada pelo tempo do frame.
        Vector2 newPosition = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
        
        // 3. MovePosition diz para o motor físico mover a raquete exatamente para essa coordenada calculada
        Rb.MovePosition(newPosition);
    }
}