using UnityEngine;

public enum TutorialAction { None, ForceDistraction, ForceStress, ReleasePuppets, ForceDialog } 

[CreateAssetMenu(fileName = "TutorialStep_", menuName = "MAP Simulator/Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [Header("--- 1. TEXTO Y TIEMPO ---")]
    [TextArea(3, 5)]
    public string dialogueText;
    [Tooltip("¿Cuántos segundos tarda en aparecer esta tarjeta después de la anterior?")]
    public float delayBeforeShowing = 0f;
    [Tooltip("¿Cuántos segundos dura en pantalla antes de avanzar sola? (Pon 0 para que espere el clic)")]
    public float autoAdvanceDuration = 0f;

    [Space(15)]
    [Header("--- 2. CONTROL DEL JUEGO ---")]
    public bool pausesGame;
    public bool lockAllTools;

    [Space(15)]
    [Header("--- 3. SISTEMA DE FLECHA ---")]
    public bool showArrow;
    public bool pointToStudent;
    public string uiButtonName;
    public float arrowAngle = 180f; 

    [Space(15)]
    [Header("--- 4. ACCIONES CINEMATOGRÁFICAS (TÍTERES) ---")]
    public TutorialAction actionOnDisplay = TutorialAction.None;
    [Tooltip("El asiento (0, 1, 2...) del alumno al que le pasará la acción")]
    public int targetSeat = 0; 
    
    [Tooltip("Solo aplica si la acción es ForceDialog")]
    public string forcedBubbleText;
    public Color forcedBubbleColor = Color.white;
}