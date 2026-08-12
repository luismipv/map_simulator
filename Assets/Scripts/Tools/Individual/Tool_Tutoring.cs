using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Tool_Tutoring", menuName = "TeacherTools/Tool_Tutoring")]
public class ToolTutoring : TeacherTool
{
    [Header("Impacto Base de Asesoría")]
    public float baseLearningBoost = 4f;
    public float baseStressRelief = -2f; 

    public override void ApplyToolEffect(Student target, Logic gameLogic)
    {
        if (target.currentState == StudentState.Resting)
        {
         target.ShowBubble("Déjeme descansar profe 😴", Color.red);
         return;
        }
            
        gameLogic.StartCoroutine(PrivateTutoringRoutine(target, gameLogic, this));
    }

   private IEnumerator PrivateTutoringRoutine(Student student, Logic gameLogic, TeacherTool toolReference)
   {
        ToolManager.Instance.isTeacherBusy = true;
        UIManager.Instance.SetTeacherBusy(true);

        //Debug.Log($"Iniciando asesoría privada con {student.studentName}.");

        ToolReaction reaction = student.personalityData.GetReactionForTool(toolReference);
        float finalLearningBoost = baseLearningBoost * reaction.learningMod;
        float finalStressRelief = baseStressRelief * reaction.stressMod;
        
        // ¡NUEVO SISTEMA SEGURO!
        student.AddLearningModifier(ModifierID.Tool_Tutoring, finalLearningBoost); 
        student.AddStressModifier(ModifierID.Tool_Tutoring, finalStressRelief);             
        student.ChangeState(StudentState.Working);

        float timer = 0f;
        while (timer < 5f)
        {
            if (student == null || student.currentState == StudentState.Graduated || student.currentState == StudentState.DroppedOut)
            {
                //Debug.Log("Asesoría cancelada tempranamente: El alumno ya no está en clase.");
                break; 
            }

            timer += Time.deltaTime; 
            yield return null; 
        }

        // ¡LIMPIEZA SEGURA!
        if (student != null && student.currentState != StudentState.Graduated && student.currentState != StudentState.DroppedOut)
        {
            student.RemoveLearningModifier(ModifierID.Tool_Tutoring);
            student.RemoveStressModifier(ModifierID.Tool_Tutoring);
        }
        
        ToolManager.Instance.isTeacherBusy = false;
        UIManager.Instance.SetTeacherBusy(false);
        Debug.Log("Terminó la asesoría. El maestro vuelve a estar libre.");
    }
}