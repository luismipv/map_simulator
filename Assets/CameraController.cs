using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Configuración")]
    public float transitionSpeed = 3f;
    
    private Camera cam;
    private Vector3 targetPosition;
    private float targetSize;

    private void Awake()
    {
        // Singleton para que otros scripts lo encuentren fácilmente
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        targetSize = cam.orthographicSize;
    }

    private void Update()
    {
        // La cámara se mueve solita hacia su objetivo
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * transitionSpeed);
    }

    // Método público para recibir órdenes de otros scripts
    public void SetCameraTarget(Vector3 newPosition, float newSize)
    {
        targetPosition = newPosition;
        targetSize = newSize;
    }
}