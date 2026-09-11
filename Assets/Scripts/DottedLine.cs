using UnityEngine;

public class DottedLine : MonoBehaviour
{
    [SerializeField] private GameObject dashPrefab; // Arraste um Prefab de quadrado/retângulo branco
    [SerializeField] private int dashCount = 15;
    [SerializeField] private float totalHeight = 10f;

    private void Start()
    {
        float step = totalHeight / dashCount;
        float startY = transform.position.y - (totalHeight / 2f) + (step / 2f);

        for (int i = 0; i < dashCount; i++)
        {
            Vector3 pos = new Vector3(transform.position.x, startY + (i * step), 0);
            Instantiate(dashPrefab, pos, Quaternion.identity, transform);
        }
    }
}