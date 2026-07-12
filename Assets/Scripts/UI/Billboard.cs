using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        // Encontramos la cámara principal al iniciar
        cam = Camera.main;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = cam;
    }

    // Usamos LateUpdate en lugar de Update. 
    // Esto asegura que la UI rote DESPUÉS de que el alumno termine de moverse o animarse, evitando temblores visuales.
    void LateUpdate()
    {
        if (cam != null)
        {
            // Hace que la interfaz mire exactamente en la misma dirección que la cámara
            transform.forward = cam.transform.forward;
        }
    }
}