using UnityEngine;
using System;
using System.Collections;

[CreateAssetMenu(fileName = "Global_SurpriseExam", menuName = "Global Tools/Surprise Exam")]
public class GlobalToolSurpriseExam : GlobalTool
{
    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (ToolManager.Instance.isTeacherBusy) return;
        gameLogic.StartCoroutine(SurpriseExamRoutine(gameLogic, onFinished));
    }

    private IEnumerator SurpriseExamRoutine(Logic gameLogic, Action onFinished)
    {
        Debug.Log("¡Examen Sorpresa! Reacciones según personalidad.");
        foreach (Student s in gameLogic.allStudents)
        {
            if (s == null || s.currentState == StudentState.DroppedOut) continue;

            s.ChangeState(StudentState.Working); 

            GlobalToolReaction reaction = s.personalityData.GetReactionForGlobalTool(this);

            float learningMod = 1f;
            float stressMod = 1f;

            float finalLearningMod = learningMod * reaction.learningMod;
            float finalStressMod = stressMod * reaction.stressMod;

            // ¡NUEVO SISTEMA SEGURO! 
            s.AddLearningModifier(ModifierID.GlobalTool_Exam, finalLearningMod);
            s.AddStressModifier(ModifierID.GlobalTool_Exam, finalStressMod);
        }

        yield return new WaitForSeconds(8f); 

        Debug.Log("Fin del examen. Todo vuelve a la normalidad.");
        foreach (Student s in gameLogic.allStudents)
        {
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;
            
            // ¡LIMPIEZA SEGURA!
            s.RemoveLearningModifier(ModifierID.GlobalTool_Exam);
            s.RemoveStressModifier(ModifierID.GlobalTool_Exam);
        }
        
        onFinished?.Invoke(); 
    }
}