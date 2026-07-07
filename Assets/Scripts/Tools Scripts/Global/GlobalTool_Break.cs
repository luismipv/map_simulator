using UnityEngine;
using System;
using System.Collections;

[CreateAssetMenu(fileName = "Global_Break", menuName = "Global Tools/Break")]
public class GlobalToolBreak : GlobalTool
{
    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (ToolManager.Instance.isTeacherBusy) 
        {
            onFinished?.Invoke(); 
            return;
        }
        gameLogic.StartCoroutine(GlobalBreakRoutine(gameLogic, onFinished));
    }

    private IEnumerator GlobalBreakRoutine(Logic gameLogic, Action onFinished)
    {
        Debug.Log("¡Recreo General! Todos desaparecen.");
        AudioManager.Instance.PostEvent("GlobalTools_Break", null); //SONIDO
        
        // 1. DESAPARECER (Sin curarlos todavía)
        foreach (Student s in gameLogic.allStudents)
        {
            // Chaleco antibalas: ignoramos a los que ya no están en clase
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;
            
            s.gameObject.SetActive(false); 
        }

        // Esperamos el tiempo del recreo
        yield return new WaitForSeconds(10f); 

        Debug.Log("Fin del recreo. Todos vuelven a sus lugares.");
        
        // 2. REAPARECER Y APLICAR EFECTOS
        foreach (Student s in gameLogic.allStudents)
        {
            // Volvemos a revisar por seguridad
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;
            
            s.gameObject.SetActive(true); 
            s.ChangeState(StudentState.Working); 
            
            // ¡Curamos al alumno AHORA! Como ya está vivo y conectado, el texto flotará perfecto
            s.ModifyStressInstant(-40f); 
        }
        
        onFinished?.Invoke(); 
    }
}