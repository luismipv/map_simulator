using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Cámaras a Sincronizar")]
    public Camera camPrincipal; // Se asigna sola, pero puedes arrastrar tu Main Camera
    public Camera camUI;        // ¡Arrastra aquí tu Camara_UI desde el Inspector!

    [Header("Configuración")]
    public float transitionSpeed = 3f;
    
    private Vector3 targetPosition;
    private float targetSize;

    private void Awake()
    {
        // El Singleton de siempre, protegiendo que solo haya un controlador
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        if (camPrincipal == null) camPrincipal = GetComponent<Camera>();
        
        targetPosition = transform.position;
        if (camPrincipal != null) targetSize = camPrincipal.orthographicSize;
    }

    private void Update()
    {
        if (camPrincipal == null) return;

        // 1. Movemos la cámara principal suavemente
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);
        camPrincipal.orthographicSize = Mathf.Lerp(camPrincipal.orthographicSize, targetSize, Time.deltaTime * transitionSpeed);

        // 2. Forzamos a la cámara de UI a copiar EXACTAMENTE a la principal (sin retrasos)
        if (camUI != null)
        {
            camUI.transform.position = transform.position;
            camUI.orthographicSize = camPrincipal.orthographicSize;
        }
    }

    // Método público para recibir órdenes de otros scripts
    public void SetCameraTarget(Vector3 newPosition, float newSize)
    {
        targetPosition = newPosition;
        targetSize = newSize;
    }
}