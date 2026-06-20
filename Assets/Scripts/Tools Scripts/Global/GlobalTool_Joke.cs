using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Global_Joke", menuName = "Global Tools/Joke")]
public class GlobalToolJoke : GlobalTool
{
    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (gameLogic.isTeacherBusy) return;
        Debug.Log("Lanzando chiste global...");

        // Iteramos sobre la lista de alumnos que vive en Logic
        foreach (Student s in gameLogic.allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) continue; 

            bool likedIt = false;

            switch (s.personalityData.personalityType)
            {
                case StudentPersonality.Slacker:
                    s.ModifyStressInstant(-30f);
                    likedIt = true;
                    break;
                    
                case StudentPersonality.Nerd:
                    if (UnityEngine.Random.Range(0f, 100f) < 10f) { s.ModifyStressInstant(-15f); likedIt = true; }
                    else { s.ModifyStressInstant(25f); likedIt = false; }
                    break;
                    
                default:
                    if (UnityEngine.Random.Range(0f, 100f) <= 70f) { s.ModifyStressInstant(-25f); likedIt = true; }
                    else { s.ModifyStressInstant(20f); likedIt = false; }
                    break;
            }

            s.RequestJokeFeedback(likedIt);
        }
        onFinished?.Invoke(); // Avisamos que el efecto terminó para que el botón se reactive
    }
}