using UnityEngine;
using System.Collections.Generic;

public class DistractionManager : MonoBehaviour
{
    public static DistractionManager Instance { get; private set; }

    [Header("Sistema de Distracción Espacial")]
    public float contagionRadius = 5f; // <-- ¡Te lo ajusté a 5f, 250f era gigante para 3D!

    private Logic gameLogic;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        gameLogic = Logic.Instance != null ? Logic.Instance : Object.FindAnyObjectByType<Logic>();
    }

    public void TryInfectStudent(Student source)
    {
        if (gameLogic == null || gameLogic.allStudents.Count == 0) return;

        List<Student> infectable = new List<Student>();
        
        foreach (Student s in gameLogic.allStudents)
        {
            if (s != source && (s.currentState == StudentState.Working || s.currentState == StudentState.Resting))
            {
                // ¡CORRECCIÓN CRÍTICA! Aplanamos la Y y usamos Vector3 para medir bien en 3D
                Vector3 posSource = new Vector3(source.transform.position.x, 0, source.transform.position.z);
                Vector3 posS = new Vector3(s.transform.position.x, 0, s.transform.position.z);
                
                float distance = Vector3.Distance(posSource, posS);
                if (distance <= contagionRadius) infectable.Add(s);
            }
        }
        
        if (infectable.Count > 0)
        {
            int randomIndex = Random.Range(0, infectable.Count);
            Student target = infectable[randomIndex];

            if (Random.Range(0f, 100f) <= 40f) 
            {
                target.ChangeState(StudentState.Distracted);
                source.ShowBubble("Te tengo chisme...", Color.orange);
                source.ShowFloatingText($"¡El chisme pegó! {source.studentName} distrajo a su vecino {target.studentName}",Color.white);
                target.ShowBubble("Cuenta!", Color.orange);
                AudioManager.Instance.PostEvent("Student_Distraction_Successful", target.gameObject); //SONIDO
                TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentDistractedByOtherStudent);
                //Debug.Log($"¡El chisme pegó! {source.studentName} distrajo a su vecino {target.studentName}");
            }
            else
            {
                target.ShowBubble("¡Déjame Trabajar!", Color.orange);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Encontramos a los alumnos directo en la escena para que el Gizmo funcione sin darle Play
        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        if (todosLosAlumnos == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // Naranja para el chisme
        foreach (Student student in todosLosAlumnos)
        {
            // Solo dibujamos la nube tóxica de distracción si el alumno está realmente distraído
            if (student.gameObject.activeSelf && student.currentState == StudentState.Distracted)
            {
                Vector3 pisoPos = new Vector3(student.transform.position.x, 0, student.transform.position.z);
                Gizmos.DrawWireSphere(pisoPos, contagionRadius);
            }
        }
    }
}