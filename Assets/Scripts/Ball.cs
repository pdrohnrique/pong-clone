using UnityEngine;

public class Ball : MonoBehaviour
{
    //sugestões de melhora:
    //paddle grip, aumentar força do rebote, aumentar velocidade com o tempo
    public float speed = 100f;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    
    private void Start()
    {
        AddStartingForce();
    }

    private void AddStartingForce()
    {
        float x = Random.value < 0.5f ? -1f : 1f;
        float y = Random.value < 0.5f ? Random.Range(-1f, -0.5f) : Random.Range(0.5f, 1f);
        
        Vector2 direction = new Vector2(x, y);
        _rb.AddForce(direction * speed);
    }
}