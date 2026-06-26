using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Tool_Tutoring", menuName = "TeacherTools/Tool_Tutoring")]
public class ToolTutoring : TeacherTool
{
    [Header("Impacto Base de Asesoría")]
    public float baseLearningBoost = 4f;
    public float baseStressRelief = -2f; // Negativo porque reduce el estrés

    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        if (target.currentState == StudentState.Resting) return;
        
        // Le pedimos a Logic que ejecute nuestra corrutina, pasándole "this" (esta herramienta)
        gameLogic.StartCoroutine(PrivateTutoringRoutine(target, gameLogic, this));
    }

   private IEnumerator PrivateTutoringRoutine(Student student, Logic gameLogic, TeacherTool toolReference)
    {
    // Ocupamos al maestro
    gameLogic.isTeacherBusy = true;
    UIManager.Instance.SetTeacherBusy(true);

    Debug.Log($"Iniciando asesoría privada con {student.studentName}.");

    // 1. Calculamos la reacción
    ToolReaction reaction = student.personalityData.GetReactionForTool(toolReference);
    float finalLearningBoost = baseLearningBoost * reaction.learningMod;
    float finalStressRelief = baseStressRelief * reaction.stressMod;
    
    // 2. Aplicamos Buffs
    student.activeLearningBuffs.Add("Asesoría 🧠", finalLearningBoost); 
    student.activeStressBuffs.Add("Asesoría 💢", finalStressRelief);             
    student.ChangeState(StudentState.Working);

    // 3. ¡EL NUEVO TEMPORIZADOR INTERRUMPIBLE!
    float timer = 0f;
    while (timer < 5f)
    {
        // Si el alumno es destruido, se gradúa o renuncia, cortamos el reloj de inmediato
        if (student == null || student.currentState == StudentState.Graduated || student.currentState == StudentState.DroppedOut)
        {
            Debug.Log("Asesoría cancelada tempranamente: El alumno ya no está en clase.");
            break; // Esto rompe el ciclo while al instante
        }

        timer += Time.deltaTime; // Sumamos el tiempo que tardó este frame
        yield return null; // Esperamos al siguiente frame para volver a revisar
    }

    // 4. LIMPIEZA TOTAL (Validando que el alumno siga vivo)
    if (student != null && student.currentState != StudentState.Graduated && student.currentState != StudentState.DroppedOut)
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