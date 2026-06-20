using UnityEngine;
using System;
using System.Collections;

[CreateAssetMenu(fileName = "Global_Break", menuName = "Global Tools/Break")]
public class GlobalToolBreak : GlobalTool
{
    public override void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished)
    {
        if (gameLogic.isTeacherBusy) 
        {
            onFinished?.Invoke(); // Si el profe está ocupado, liberamos el botón inmediatamente
            return;
        }
        gameLogic.StartCoroutine(GlobalBreakRoutine(gameLogic, onFinished));
    }

    private IEnumerator GlobalBreakRoutine(Logic gameLogic, Action onFinished)
    {
        Debug.Log("¡Recreo General! Todos desaparecen.");
        foreach (Student s in gameLogic.allStudents)
        {
            s.ModifyStressInstant(-40f); 
            s.gameObject.SetActive(false); 
        }

        yield return new WaitForSeconds(10f); 

        Debug.Log("Fin del recreo. Todos vuelven a sus lugares.");
        foreach (Student s in gameLogic.allStudents)
        {
            s.gameObject.SetActive(true); 
            s.ChangeState(StudentState.Working); 
        }
        onFinished?.Invoke(); // Avisamos que el efecto terminó para que el botón se reactive
    }
}