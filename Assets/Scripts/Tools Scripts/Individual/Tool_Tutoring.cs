using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Tool_Tutoring", menuName = "TeacherTools/Tool_Tutoring")]
public class ToolTutoring : TeacherTool
{
    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        if (target.currentState == StudentState.Resting) return;
        
        // Le pedimos a Logic que ejecute nuestra corrutina
        gameLogic.StartCoroutine(PrivateTutoringRoutine(target, gameLogic));
    }

    private IEnumerator PrivateTutoringRoutine(Student student, Logic gameLogic)
    {
        // Ocupamos al maestro y mostramos la UI usando las referencias de Logic
        gameLogic.isTeacherBusy = true;
        UIManager.Instance.SetTeacherBusy(true);

        Debug.Log($"Iniciando asesoría privada con {student.studentName}. El maestro estará ocupado por 5s.");

        // Bonificaciones según la personalidad
        float learningBoost = (student.personalityData.personalityType == StudentPersonality.Anxious) ? 10f : 4f;
        student.toolLearningMultiplier = learningBoost;
        student.toolStressMultiplier = -2f; // El estrés baja durante la asesoría

        student.ChangeState(StudentState.Working);

        // Esperamos 5 segundos
        yield return new WaitForSeconds(5f);

        // Devolvemos todo a la normalidad si el alumno sigue en el salón
        if (student.currentState != StudentState.Graduated && student.currentState != StudentState.DroppedOut)
        {
            student.toolLearningMultiplier = 1f;
            student.toolStressMultiplier = 1f;
            student.stressMultiplier = 1f;
        }

        // Liberamos al maestro
        gameLogic.isTeacherBusy = false;
        UIManager.Instance.SetTeacherBusy(false);
        Debug.Log("Terminó la asesoría. El maestro vuelve a estar libre.");
    }
}