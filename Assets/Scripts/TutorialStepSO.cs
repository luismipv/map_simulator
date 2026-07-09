using UnityEngine;

[CreateAssetMenu(fileName = "PasoTutorial_", menuName = "MAP Simulator/Paso de Tutorial")]
public class TutorialStepSO : ScriptableObject
{
    [Header("Diálogo")]
    [TextArea(3, 5)]
    [Tooltip("El texto que aparecerá en la ventana de UI")]
    public string dialogueText;

    [Header("Control de Flujo")]
    [Tooltip("Si está activo, el tiempo (Time.timeScale) se detendrá mientras se lee esto")]
    public bool pausesGame = true;
    
    [Header("Restricciones del Maestro")]
    [Tooltip("Si está activo, el maestro no podrá usar las herramientas que tenga en su mazo")]
    public bool lockAllTools = true;
}