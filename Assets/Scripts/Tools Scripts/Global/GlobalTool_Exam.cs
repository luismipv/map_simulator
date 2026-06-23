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
            // Chaleco antibalas inicial: verificar que el alumno exista y no esté quemado
            if (s == null || s.currentState == StudentState.DroppedOut) continue;

            s.ChangeState(StudentState.Working); 

            float learningMod = 1f;
            float stressMod = 1f;

            switch (s.personalityData.personalityType)
            {
                case StudentPersonality.Nerd:
                    learningMod = 3.5f; 
                    stressMod = 1.5f;   
                    break;
                case StudentPersonality.Slacker:
                    learningMod = 1f;   
                    stressMod = 1.2f;   
                    break;
                case StudentPersonality.Anxious:
                    learningMod = 1.5f; 
                    stressMod = 4f;     
                    break;
                default: 
                    learningMod = 2f; 
                    stressMod = 2f;
                    break;
            }

            // ¡NUEVO SISTEMA! Agregamos los modificadores al diccionario en lugar de sobrescribir 
            //Se puso este método diferente para evitar crasheos!
            s.activeLearningBuffs["Examen 🧠"] = learningMod;
            s.activeStressBuffs["Examen 💢"] = stressMod;
        
        }

        yield return new WaitForSeconds(8f); 

        Debug.Log("Fin del examen. Todo vuelve a la normalidad.");
        foreach (Student s in gameLogic.allStudents)
        {
            // Chaleco antibalas final: Si se graduó o se quemó DURANTE los 8 segundos, lo ignoramos
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;
            
            // ¡NUEVO SISTEMA! Solo removemos la llave "Examen" y la matemática se arregla sola
            s.activeLearningBuffs.Remove("Examen");
            s.activeStressBuffs.Remove("Examen 💢");
        }
        
        onFinished?.Invoke(); // Avisamos que el efecto terminó para que el botón se reactive
    }
}