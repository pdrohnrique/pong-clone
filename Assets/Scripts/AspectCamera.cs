using UnityEngine;

[ExecuteAlways]
public class AspectCamera : MonoBehaviour
{
    public float targetAspectWidth = 16f;
    public float targetAspectHeight = 9f;
    public float targetOrthographicSize = 5f; // Ajuste para o tamanho original da sua câmera

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        AdjustCamera();
    }

    void Update()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {
        if (_cam == null) return;

        float targetAspect = targetAspectWidth / targetAspectHeight;
        float currentAspect = Screen.width / (float)Screen.height;

        // Se a janela ficar mais estreita que 16:9, expande o Size vertical 
        // para garantir que a largura total (e as raquetes) continue visível
        if (currentAspect < targetAspect)
        {
            _cam.orthographicSize = targetOrthographicSize * (targetAspect / currentAspect);
        }
        else
        {
            _cam.orthographicSize = targetOrthographicSize;
        }
    }
}