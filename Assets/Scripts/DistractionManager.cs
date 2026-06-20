using UnityEngine;
using System.Collections.Generic;

public class DistractionManager : MonoBehaviour
{
    // ¡Nuestro Singleton para que los alumnos lo encuentren fácil!
    public static DistractionManager Instance { get; private set; }

    [Header("Sistema de Distracción Espacial")]
    public float contagionRadius = 250f; 

    private Logic gameLogic;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start()
    {
        // Buscamos al Logic original para poder leer su lista de alumnos
        gameLogic = Object.FindAnyObjectByType<Logic>();
    }

    public void TryInfectStudent(Student source)
    {
        if (gameLogic == null || gameLogic.allStudents.Count == 0) return;

        List<Student> infectable = new List<Student>();
        
        foreach (Student s in gameLogic.allStudents)
        {
            if (s != source && (s.currentState == StudentState.Working || s.currentState == StudentState.Resting))
            {
                float distance = Vector2.Distance(source.transform.position, s.transform.position);
                
                if (distance <= contagionRadius)
                {
                    infectable.Add(s);
                }
            }
        }
        
        if (infectable.Count > 0)
        {
            int randomIndex = Random.Range(0, infectable.Count);
            Student target = infectable[randomIndex];

            if (Random.Range(0f, 100f) <= 40f) 
            {
                target.ChangeState(StudentState.Distracted);
                source.RequestDistractionFeedback(true, target.studentName);
                target.RequestDistractionFeedback(true, source.studentName);
                Debug.Log($"¡El chisme pegó! {source.studentName} distrajo a su vecino {target.studentName}");
            }
            else
            {
                target.RequestDistractionFeedback(false, source.studentName);
            }
        }
    }

    // Mudamos los Gizmos visuales para acá también
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || gameLogic == null || gameLogic.allStudents == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); 
        foreach (Student student in gameLogic.allStudents)
        {
            if (student.gameObject.activeSelf && student.currentState == StudentState.Distracted)
            {
                Gizmos.DrawWireSphere(student.transform.position, contagionRadius);
            }
        }
    }
}