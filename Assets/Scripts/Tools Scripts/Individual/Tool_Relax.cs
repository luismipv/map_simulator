using UnityEngine;

[CreateAssetMenu(fileName = "Tool_Relax", menuName = "TeacherTools/Tool_Relax")]
public class ToolRelax : TeacherTool
{
    [Header("Impacto Inmediato")]
    public float baseInstantStressRelief = -10f; // Un alivio base por la buena noticia (negativo baja el estrés)

    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        // 1. Verificamos que no esté ya descansando
        if (target.currentState == StudentState.Resting) return; 
        
        // 2. Verificamos el cooldown interno del alumno
        if (target.currentRestCooldown > 0f)
        {
            Debug.Log($"{target.studentName} ya descansó hace poco. ¡A trabajar!");
            return; 
        }

        // 2.5 ¡LA MAGIA! Le preguntamos a su personalidad
        ToolReaction reaction = target.personalityData.GetReactionForTool(this);

        // Calculamos el impacto instantáneo. 
        // Ej: -10 * 1.5 (Flojo) = -15 de estrés. 
        // Ej: -10 * -0.5 (Nerd, modificador negativo) = +5 de estrés (¡Se enoja porque lo interrumpes!).
        float finalInstantStress = baseInstantStressRelief * reaction.stressMod;
        
        if (finalInstantStress != 0f)
        {
            target.ModifyStressInstant(finalInstantStress);
        }

        // 3. Lo mandamos a descansar
        target.ChangeState(StudentState.Resting);

        // 4. Limpiamos su racha de tareas en el diccionario global
        if (gameLogic.homeworkStreak.ContainsKey(target))
        {
            gameLogic.homeworkStreak[target] = 0;
            Debug.Log($"Rendimiento de tarea de {target.studentName} restablecido al 100%. Estrés inmediato: {finalInstantStress}");
        }
        else
        {
            Debug.Log($"{target.studentName} fue enviado a descansar. Estrés inmediato: {finalInstantStress}");
        }
    }
}