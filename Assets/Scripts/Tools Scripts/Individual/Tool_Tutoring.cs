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
        // Ocupamos al maestro
        gameLogic.isTeacherBusy = true;
        UIManager.Instance.SetTeacherBusy(true);

        Debug.Log($"Iniciando asesoría privada con {student.studentName}. El maestro estará ocupado por 5s.");

        // 1. CALCULAMOS Y APLICAMOS LOS BUFFS DIRECTO AL DICCIONARIO
        float learningBoost = (student.personalityData.personalityType == StudentPersonality.Anxious) ? 10f : 4f;
        
        student.activeLearningBuffs.Add("Asesoría 🧠", learningBoost); // Sube el aprendizaje
        student.activeStressBuffs.Add("Asesoría 💢", -2f);             // Reduce el estrés

        student.ChangeState(StudentState.Working);

        // Esperamos 5 segundos
        yield return new WaitForSeconds(5f);

        // 2. LIMPIEZA TOTAL (Si el alumno sigue vivo/en el salón)
        if (student.currentState != StudentState.Graduated && student.currentState != StudentState.DroppedOut)
        {
            student.activeLearningBuffs.Remove("Asesoría 🧠");
            student.activeStressBuffs.Remove("Asesoría 💢");
        }

        // Liberamos al maestro
        gameLogic.isTeacherBusy = false;
        UIManager.Instance.SetTeacherBusy(false);
        Debug.Log("Terminó la asesoría. El maestro vuelve a estar libre.");
    }
}