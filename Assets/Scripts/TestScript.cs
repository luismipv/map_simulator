using UnityEngine;

public class TestEvaluation : MonoBehaviour
{
    public EvaluationScreenManager evalManager;

    [ContextMenu("Iniciar Animación de Evaluación")]
    public void RunTestDificil()
    {
        // Alumno aprueba el examen pero el estrés le da un ataque de pánico
       // evalManager.StartEvaluationAnimation("Luismi (Prueba)", 85f, 90f, 60, ExamPenaltyMode.PanicAttack);
        Debug.Log("Test de Evaluación Difícil iniciado.");
    }
}