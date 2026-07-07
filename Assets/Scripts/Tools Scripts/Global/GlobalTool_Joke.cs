using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Global_Joke", menuName = "Global Tools/Joke")]
public class GlobalToolJoke : GlobalTool
{
    [Header("Impactos Base del Chiste")]
    public float baseSuccessStressRelief = -25f; // Lo que cura si da risa
    public float baseFailStressDamage = 20f;     // Lo que daña si da pena

    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (ToolManager.Instance.isTeacherBusy) return;
        Debug.Log("Lanzando chiste global...");

        foreach (Student s in gameLogic.allStudents)
        {
            if (s == null || s.currentState == StudentState.DroppedOut) continue; 

            // 1. Pedimos la reacción de ESTE alumno
            GlobalToolReaction reaction = s.personalityData.GetReactionForGlobalTool(this);

            // 2. Tiramos los dados (0 al 100)
            float roll = UnityEngine.Random.Range(0f, 100f);
            
            // Si el dado cayó por debajo de su chance, ¡le gustó!
            bool likedIt = (roll <= reaction.successChance);

            // 3. Aplicamos el efecto según el resultado
            if (likedIt)
            {
                // Multiplicamos el alivio (-25) por su modificador
                float finalRelief = baseSuccessStressRelief * reaction.stressMod;
                s.ModifyStressInstant(finalRelief);
            }
            else
            {
                // Multiplicamos el daño (20) por su modificador
                float finalDamage = baseFailStressDamage * reaction.stressMod;
                s.ModifyStressInstant(finalDamage);
            }

            // 4. Activamos tu feedback visual
            s.RequestJokeFeedback(likedIt);
        }
        
        onFinished?.Invoke(); 
    }
}