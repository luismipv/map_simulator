using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class SynergyVisuals : MonoBehaviour
{
    [Header("Debe coincidir con SpatialManager")]
    public float synergyRadius = 3.5f; 

    private LineRenderer lineRenderer;
    private Student myStudent;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        myStudent = GetComponent<Student>();
        
        // Configuramos la línea para que sea delgada y verde
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.startColor = Color.green; 
        lineRenderer.endColor = Color.green;
    }

    void Update()
    {
        DrawSynergyWeb();
    }

    private void DrawSynergyWeb()
    {
        // 1. Buscamos a todos los alumnos en escena
        Student[] allStudents = FindObjectsByType<Student>(FindObjectsSortMode.None);
        List<Vector3> linePoints = new List<Vector3>();

        // 2. El punto de origen es este estudiante. 
        // Le restamos 1 en Z para que la línea flote hacia la cámara y no se entierre en los escritorios
        Vector3 originPoint = transform.position + new Vector3(0, 0, -1f); 
        linePoints.Add(originPoint);

        // 3. Medimos las distancias usando la verdadera distancia 3D en el piso
        foreach (Student3D other in allStudents)
        {
            // OJO: Cambia Student3D por Student si así se llama tu script principal
            if (other == myStudent || other.currentState == StudentState.DroppedOut || other.currentState == StudentState.Graduated) continue;

            // Aplanamos las posiciones para que el radar no se rompa al levantar al alumno
            Vector3 miPosPiso = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 otraPosPiso = new Vector3(other.transform.position.x, 0, other.transform.position.z);

            // Ahora sí, usamos Vector3.Distance
            float distance = Vector3.Distance(miPosPiso, otraPosPiso);
            
            if (distance <= synergyRadius)
            {
                // Si está en rango, tiramos el cable hasta su pecho
                Vector3 targetPoint = other.transform.position + new Vector3(0, 0.5f, 0); // Ajusté un poco para que apunte al pecho
                linePoints.Add(targetPoint);
                
                // Y regresamos el cable al origen
                linePoints.Add(originPoint); 
            }
        }

        // 4. Dibujamos la red
        if (linePoints.Count <= 1)
        {
            lineRenderer.positionCount = 0; // Apagamos la línea si no hay nadie cerca
        }
        else
        {
            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray()); // Trazamos los puntos
        }
    }
}