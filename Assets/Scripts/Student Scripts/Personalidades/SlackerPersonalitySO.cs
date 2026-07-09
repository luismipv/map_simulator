using UnityEngine;

// Fíjate en el menú: lo ponemos en una subcarpeta para tener orden
[CreateAssetMenu(fileName = "SlackerPersonality", menuName = "Scriptable Objects/Personalities/Slacker")]
public class SlackerPersonalitySO : StudentPersonalitySO 
{
    // Usamos 'override' para inyectar su lógica única de distracción y pánico
    public override void EvaluateSpecialBehaviors(Student student, bool distractionsEnabled)
    {
        if (student.currentState == StudentState.Working)
        {
            // 1. Se distrae si está relajado y el nivel lo permite
            if (student.stressLevel < 40f && distractionsEnabled)
            {
                if (Random.value < 0.08f * Time.deltaTime)
                {
                    student.ChangeState(StudentState.Distracted);
                    student.ShowBubble("Un descansito...", Color.orange);
                }
            }
            // 2. Acelerón de Pánico si se estresa mucho
            else if (student.stressLevel >= 80f)
            {
                student.AddLearningModifier(ModifierID.Panico, 2.0f);
            }
            // 3. Limpieza
            else
            {
                student.RemoveLearningModifier(ModifierID.Panico);
            }
        }
    }
}