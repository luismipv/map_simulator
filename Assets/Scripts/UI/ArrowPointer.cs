using UnityEngine;

public class ArrowPointer : MonoBehaviour
{
    public static ArrowPointer Instance { get; private set; }

    [Header("Configuración Visual")]
    [Tooltip("Distancia extra base para que la flecha no tape el objeto")]
    public Vector3 offset2D = new Vector3(0, 100f, 0); 
    public Vector3 offset3D = new Vector3(0, 150f, 0); 

    [Header("Animación (Oscilación)")]
    [Tooltip("Qué tan rápido rebota la flecha")]
    public float bounceSpeed = 5f;
    [Tooltip("Qué tan alto rebota la flecha (en píxeles)")]
    public float bounceHeight = 15f;

    private RectTransform arrowRect;
    private RectTransform target2D; 
    private Transform target3D;     

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        arrowRect = GetComponent<RectTransform>();
        HideArrow(); 
    }

    private void Update()
    {
        // 1. Calculamos el valor del rebote oscilatorio (positivo y negativo)
        float bounceOffset = Mathf.Sin(Time.unscaledTime * bounceSpeed) * bounceHeight;
        
        // 2. ¡LA MAGIA DE UNITY! 
        // Multiplicamos el rebote por el "Arriba Local" de la flecha.
        // Si está a 0 grados, arrowRect.up es (0, 1, 0) -> Rebota en Y
        // Si está a 90 grados, arrowRect.up es (-1, 0, 0) -> Rebota en X
        Vector3 animatedOffset = arrowRect.up * bounceOffset;

        // 3. Aplicamos la posición según si es 2D o 3D
        if (target2D != null)
        {
            arrowRect.position = target2D.position + offset2D + animatedOffset;
        }
        else if (target3D != null && Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target3D.position);
            arrowRect.position = screenPos + offset3D + animatedOffset + Vector3.up*50f;
        }
    }

    public void PointToUI(RectTransform uiElement, float zRotation)
    {
        target3D = null;
        target2D = uiElement;
        
        // ¡Giramos la flecha!
        arrowRect.localEulerAngles = new Vector3(0, 0, zRotation);
        
        gameObject.SetActive(true);
    }

    public void PointTo3D(Transform worldObject, float zRotation)
    {
        target2D = null;
        target3D = worldObject;
        
        // ¡Giramos la flecha!
        arrowRect.localEulerAngles = new Vector3(0, 0, zRotation);
        
        gameObject.SetActive(true);
    }

    public void HideArrow()
    {
        target2D = null;
        target3D = null;
        gameObject.SetActive(false);
    }
}