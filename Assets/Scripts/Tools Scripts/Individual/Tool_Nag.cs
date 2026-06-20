using UnityEngine;

[CreateAssetMenu(fileName = "Tool_Nag", menuName = "TeacherTools/Tool_Nag")]
public class ToolNag : TeacherTool
{
    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        // Solo si el alumno está distraído, el regaño tiene su efecto ideal
        if (target.currentState == StudentState.Distracted)
        {
            target.ModifyStressInstant(10f); 
            target.ChangeState(StudentState.Working); // Lo regañas y vuelve a trabajar
            Debug.Log($"¡Regañaste a {target.studentName}!");
        }
        else if (target.currentState == StudentState.Resting)
        {
            target.ModifyStressInstant(25f); // Regañar en el recreo es muy tóxico
            Debug.Log($"¡{target.studentName} está descansando! Regañarlo lo estresa mucho.");
        }
        else
        {
            target.ModifyStressInstant(20f); // Regañar a alguien que ya estaba trabajando
            Debug.Log($"Intentaste regañar a {target.studentName}, pero no estaba distraído.");
        }
    }
}