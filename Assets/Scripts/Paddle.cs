using UnityEngine;

public class Paddle : MonoBehaviour
{
    [SerializeField] protected float speed;
    protected Rigidbody2D Rb;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
    }
}
