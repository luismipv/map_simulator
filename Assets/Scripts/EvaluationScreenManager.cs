using UnityEngine;
using System.Collections.Generic;

public class EvaluationScreenManager : MonoBehaviour
{
    [Header("Configuración de Filas")]
    public GameObject studentRowPrefab; 
    public Transform listContainer;     

    public void ShowAllResults(List<StudentEvalData> students, int passingQuota)
    {
        // 1. Limpiamos filas viejas
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Creamos una fila por cada alumno
        foreach (StudentEvalData student in students)
        {
            GameObject newRow = Instantiate(studentRowPrefab, listContainer);
            StudentResultRow rowScript = newRow.GetComponent<StudentResultRow>();
            rowScript.Animate(student, passingQuota);
        }
    }

    // ==========================================
    // --- HERRAMIENTA DE PRUEBA RÁPIDA ---
    // ==========================================
    [ContextMenu("Probar Animación de Lista")]
    public void TestAnimations()
    {
        // Creamos una lista simulada para no tener que jugar todo el ciclo
        List<StudentEvalData> dummyStudents = new List<StudentEvalData>
        {
            new StudentEvalData { studentName = "Luismi (Test)", rawLearning = 85f, rawStress = 90f, penaltyMode = ExamPenaltyMode.PanicAttack, isGraduated = false },
            new StudentEvalData { studentName = "Diego (Test)", rawLearning = 95f, rawStress = 30f, penaltyMode = ExamPenaltyMode.MoneyFine, isGraduated = false },
            new StudentEvalData { studentName = "María (Test)", rawLearning = 55f, rawStress = 85f, penaltyMode = ExamPenaltyMode.Snowball, isGraduated = false }
        };

        ShowAllResults(dummyStudents, 60);
    }
}