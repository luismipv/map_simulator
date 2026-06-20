using UnityEngine;

[CreateAssetMenu(fileName = "Tool_Relax", menuName = "TeacherTools/Tool_Relax")]
public class ToolRelax : TeacherTool
{
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

        // 3. Lo mandamos a descansar
        target.ChangeState(StudentState.Resting);

        // 4. ¡NUEVO! Limpiamos su racha de tareas en el diccionario global
        if (gameLogic.homeworkStreak.ContainsKey(target))
        {
            gameLogic.homeworkStreak[target] = 0;
            Debug.Log($"Rendimiento de tarea de {target.studentName} restablecido al 100%.");
        }
        else
        {
            Debug.Log($"{target.studentName} fue enviado a descansar.");
        }
    }
}