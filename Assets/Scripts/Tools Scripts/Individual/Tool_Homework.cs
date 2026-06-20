using UnityEngine;

[CreateAssetMenu(fileName = "Tool_Homework", menuName = "TeacherTools/Tool_Homework")]
public class ToolHomework : TeacherTool
{
    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        // 1. Verificamos si el alumno está descansando
        if (target.currentState == StudentState.Resting)
        {
            Debug.Log($"{target.studentName} está en su descanso. ¡Déjalo respirar!");
            return; 
        }

        // 2. Revisamos o inicializamos su racha en el diccionario global
        if (!gameLogic.homeworkStreak.ContainsKey(target)) 
        {
            gameLogic.homeworkStreak[target] = 0;
        }

        int streak = gameLogic.homeworkStreak[target];

        // 3. Matemáticas de Rendimiento Decreciente
        // Aprendizaje: Baja 25% por tarea extra (tope mínimo de 25% de efectividad)
        float learningMultiplier = Mathf.Max(0.25f, 1f - (streak * 0.25f));
        
        // Estrés: Sube 50% por tarea extra (¡castigo por spamear!)
        float stressMultiplier = 1f + (streak * 0.5f);

        float finalLearning = 10f * learningMultiplier;
        float finalStress = 20f * stressMultiplier;

        // 4. Aplicamos los efectos al alumno
        target.ModifyStressInstant(finalStress);
        target.ModifyLearningInstant(finalLearning);
        
        // 5. Aumentamos su racha para la próxima vez
        gameLogic.homeworkStreak[target]++; 
        
        Debug.Log($"¡Tarea a {target.studentName}! Racha actual: {gameLogic.homeworkStreak[target]}. Aprendizaje: +{finalLearning}, Estrés: +{finalStress}");
    }
}
