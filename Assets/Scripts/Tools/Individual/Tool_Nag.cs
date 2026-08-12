using UnityEngine;

[CreateAssetMenu(fileName = "Tool_Nag", menuName = "TeacherTools/Tool_Nag")]
public class ToolNag : TeacherTool
{
    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        // 1. Le preguntamos a la personalidad cómo reacciona a ESTA herramienta (El Regaño)
        ToolReaction reaction = target.personalityData.GetReactionForTool(this);

        // Solo si el alumno está distraído, el regaño tiene su efecto ideal
        if (target.currentState == StudentState.Distracted)
        {
            // Multiplicamos la base (10) por su modificador de personalidad
            float finalStress = 10f * reaction.stressMod;
            
            target.ModifyStressInstant(finalStress); 
            target.ChangeState(StudentState.Working); // Lo regañas y vuelve a trabajar
            target.ShowBubble($"¡Ya me pongo a trabajar!",Color.orange);
            AudioManager.Instance.PostEvent("Student_Nag"); //SONIDO
            //Debug.Log($"¡Regañaste a {target.studentName}! Estrés: +{finalStress}");
        }
        else if (target.currentState == StudentState.Resting)
        {
            // Multiplicamos la base tóxica (25) por su modificador
            float finalStress = 25f * reaction.stressMod;
            
            target.ModifyStressInstant(finalStress); // Regañar en el recreo es muy tóxico
            target.ShowBubble($"¡Estoy descansando!",Color.orange);
            //Debug.Log($"¡{target.studentName} está descansando! Regañarlo lo estresa mucho. Estrés: +{finalStress}");
        }
        else
        {
            // Multiplicamos la base de error (20) por su modificador
            float finalStress = 20f * reaction.stressMod;
            
            target.ModifyStressInstant(finalStress); // Regañar a alguien que ya estaba trabajando
            target.ShowBubble($"¡No estoy distraído!",Color.orange);
            //Debug.Log($"Intentaste regañar a {target.studentName}, pero no estaba distraído. Estrés: +{finalStress}");
        }
        
        // (Opcional) Si la personalidad tiene un modificador de aprendizaje al ser regañado
        // (Ej: El Flojo o Bully que aprenden de golpe por el susto)
        // float finalLearning = 5f * reaction.learningMod;
        // target.ModifyLearningInstant(finalLearning);
    }
}