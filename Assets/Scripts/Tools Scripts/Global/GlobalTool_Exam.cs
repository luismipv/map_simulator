using UnityEngine;
using System;
using System.Collections;

[CreateAssetMenu(fileName = "Global_SurpriseExam", menuName = "Global Tools/Surprise Exam")]
public class GlobalToolSurpriseExam : GlobalTool
{
    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (gameLogic.isTeacherBusy) return;
        gameLogic.StartCoroutine(SurpriseExamRoutine(gameLogic, onFinished));
    }

    private IEnumerator SurpriseExamRoutine(Logic gameLogic, Action onFinished)
    {
        Debug.Log("¡Examen Sorpresa! Reacciones según personalidad.");
        foreach (Student s in gameLogic.allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) continue;

            s.ChangeState(StudentState.Working); 

            switch (s.personalityData.personalityType)
            {
                case StudentPersonality.Nerd:
                    s.learningMultiplier = 3.5f; 
                    s.stressMultiplier = 1.5f;   
                    break;
                case StudentPersonality.Slacker:
                    s.learningMultiplier = 1f;   
                    s.stressMultiplier = 1.2f;   
                    break;
                case StudentPersonality.Anxious:
                    s.learningMultiplier = 1.5f; 
                    s.stressMultiplier = 4f;     
                    break;
                default: 
                    s.learningMultiplier = 2f; 
                    s.stressMultiplier = 2f;
                    break;
            }
        }

        yield return new WaitForSeconds(8f); 

        Debug.Log("Fin del examen. Todo vuelve a la normalidad.");
        foreach (Student s in gameLogic.allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) continue;
            
            s.learningMultiplier = 1f; 
            s.stressMultiplier = 1f;
        }
        onFinished?.Invoke(); // Avisamos que el efecto terminó para que el botón se reactive
    }
}