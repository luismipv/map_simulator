using UnityEngine;

public class TutorialStudent : Student3D
{
    [Header("Control de Tutorial")]
    [Tooltip("Si está activo, este alumno no cambiará de estado automáticamente. Solo por orden del TutorialManager.")]
    public bool isPuppet = true;

    // ==================================================
    // 1. SECUESTRO DEL LIBRE ALBEDRÍO
    // ==================================================
    protected override void CheckAutomaticTransitions()
    {
        // Si NO es un títere, que se comporte como un alumno 3D normal
        if (!isPuppet)
        {
            base.CheckAutomaticTransitions();
            return;
        }

        if (learningLevel >= maxLearning && currentState != StudentState.Finished && currentState != StudentState.Graduated) 
        { 
            ChangeState(StudentState.Finished); 
            return; 
        }
        
        if (stressLevel >= maxStress && currentState != StudentState.Burnout) 
        { 
            ChangeState(StudentState.Burnout); 
            return; 
        }

        if (currentState == StudentState.Working && learningLevel > 50f && stressLevel >= 60f && stressLevel < 75f) 
        {
            ChangeState(StudentState.Flow);
        }

        // ¡SI ES TÍTERE, NO HACEMOS NADA! 
        // Ignoramos el burnout automático, las distracciones por probabilidad, etc.
        // El alumno se quedará en su estado actual para siempre hasta que lo forcemos.
    }

    // ==================================================
    // 2. COMANDOS DIRECTOS (Para tu TutorialManager)
    // ==================================================
    
    // Llamas a esta función desde el TutorialManager cuando quieras enseñar el Regaño
    public void ForceDistraction()
    {
        if (currentState != StudentState.Distracted)
        {
            ChangeState(StudentState.Distracted);
            //ShowBubble("¡Me obligaron a distraerme!", Color.orange);
            
            if (AudioManager.Instance != null) 
                AudioManager.Instance.PostEvent("Student_Distracted", this.gameObject); 
        }
    }

    // Llamas a esta función cuando quieras enseñar el Descanso
    public void ForceBurnoutWarning()
    {
        // Le subimos el estrés al 95% de golpe para que el jugador se asuste y actúe
        stressLevel = maxStress * 0.85f;
        //ShowBubble("¡A punto de explotar!", Color.red);
        
        // Aquí podrías incluso forzar el estado de Burnout si quieres:
        // ChangeState(StudentState.Burnout);
    }
    
    // Devuélvele su libre albedrío cuando acabe el tutorial
    public void ReleasePuppet()
    {
        isPuppet = false;
        ShowBubble("¡Soy libre!", Color.green);
    }

    public void ForceDialog(string message, Color color)
    {
        ShowBubble(message, color);
    }
}