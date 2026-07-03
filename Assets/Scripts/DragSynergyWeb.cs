using UnityEngine;
using System.Collections.Generic;

public class DragSynergyWeb : MonoBehaviour
{
    [Header("Configuración")]
    public float synergyRadius = 3.5f;
    public Color colorPositivo = Color.green;
    public Color colorNegativo = Color.red;
    public Color colorNeutral = Color.white; 
    public Material materialLinea; 

    private Student currentDraggedStudent;
    private List<LineRenderer> lineasActivas = new List<LineRenderer>();

    // 1. Guardamos los gradientes aquí para no crearlos 60 veces por segundo
    private Gradient gradientePositivo;
    private Gradient gradienteNegativo;
    private Gradient gradienteNeutral;

    void Start()
    {
        // 2. Pre-fabricamos los gradientes al iniciar el juego
        gradientePositivo = CrearGradiente(colorPositivo);
        gradienteNegativo = CrearGradiente(colorNegativo);
        gradienteNeutral = CrearGradiente(colorNeutral);
    }

    // Método auxiliar para armar el degradado visual
    private Gradient CrearGradiente(Color colorBase)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(colorBase, 0f), new GradientColorKey(colorBase, 1f) },
            // Subimos la visibilidad: 30% en las puntas y 100% en el centro. ¡Ya no se volverán invisibles!
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 1f) }
        );
        return g;
    }

    void Update()
    {
        if (currentDraggedStudent != null) DrawWebFromDraggedStudent();
        else ClearLines();
    }

    public void StartDragging(Student student)
    {
        currentDraggedStudent = student;
    }

    public void StopDragging()
    {
        currentDraggedStudent = null;
        ClearLines();
    }

    private void ClearLines()
    {
        foreach(var line in lineasActivas)
        {
            if(line != null) line.gameObject.SetActive(false);
        }
    }

    private LineRenderer GetOrCreateLine(int index)
    {
        if (index < lineasActivas.Count)
        {
            lineasActivas[index].gameObject.SetActive(true);
            return lineasActivas[index];
        }

        GameObject lineObj = new GameObject("Cable Sinergia " + index);
        lineObj.transform.SetParent(transform); 
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        // --- ¡LA MAGIA DE LA CURVA POR CÓDIGO! ---
        AnimationCurve curvaGrosor = new AnimationCurve();
        curvaGrosor.AddKey(0f, 0.02f);   // Punta de inicio súper finita
        curvaGrosor.AddKey(0.5f, 2.5f); // El centro más gordito
        curvaGrosor.AddKey(1f, 0.4f);   // Punta final súper finita
        lr.widthCurve = curvaGrosor;
        // ----------------------------------------
        
        if (materialLinea != null) lr.material = materialLinea;
        else lr.material = new Material(Shader.Find("Sprites/Default")); 
        
        lineasActivas.Add(lr);
        return lr;
    }

    private void DrawWebFromDraggedStudent()
    {
        Student[] allStudents = FindObjectsByType<Student>(FindObjectsSortMode.None);
        
        Vector3 draggedPosPiso = new Vector3(currentDraggedStudent.transform.position.x, 0, currentDraggedStudent.transform.position.z);
        Vector3 originPoint = currentDraggedStudent.transform.position + new Vector3(0, 0.5f, 0);

        int lineIndex = 0; 

        foreach (Student other in allStudents)
        {
            if (other == currentDraggedStudent || other.currentState == StudentState.DroppedOut || other.currentState == StudentState.Graduated) continue;
            if (other.personalityData == null || currentDraggedStudent.personalityData == null) continue;

            Vector3 otherPosPiso = new Vector3(other.transform.position.x, 0, other.transform.position.z);
            float distance = Vector3.Distance(draggedPosPiso, otherPosPiso);
            
            if (distance <= synergyRadius)
            {
                LineRenderer lr = GetOrCreateLine(lineIndex);
                
                // 3. Le asignamos directamente el gradiente pre-fabricado que le toca
                lr.colorGradient = DetermineGradient(currentDraggedStudent.personalityData.personalityType, other.personalityData.personalityType);
                
                lr.positionCount = 2;
                lr.SetPosition(0, originPoint);
                lr.SetPosition(1, other.transform.position + new Vector3(0, 0.5f, 0));
                
                lineIndex++;
            }
        }

        for (int i = lineIndex; i < lineasActivas.Count; i++)
        {
            lineasActivas[i].gameObject.SetActive(false);
        }
    }

    // Ahora este método devuelve un Gradient en lugar de un Color
    private Gradient DetermineGradient(StudentPersonality myType, StudentPersonality neighborType)
    {
        if (SpatialManager.Instance != null && SpatialManager.Instance.reglasDeSinergia != null)
        {
            foreach (SynergyRuleSO regla in SpatialManager.Instance.reglasDeSinergia)
            {
                if (regla != null && regla.Matches(myType, neighborType))
                {
                    if (regla.personalityA == myType && regla.personalityB == neighborType)
                        return regla.learningMultA >= 1f ? gradientePositivo : gradienteNegativo;
                    
                    else if (regla.personalityA == neighborType && regla.personalityB == myType)
                        return regla.learningMultB >= 1f ? gradientePositivo : gradienteNegativo;
                }
            }
        }
        return gradienteNeutral;
    }
}