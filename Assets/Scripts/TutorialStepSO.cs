using UnityEngine;

// Tradujimos el Enum
public enum TutorialAction { None, ForceDistraction, ForceStress, ReleasePuppets, ForceDialog } 

[CreateAssetMenu(fileName = "TutorialStep_", menuName = "MAP Simulator/Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [TextArea(3, 5)]
    public string dialogueText;
    public bool pausesGame;
    public bool lockAllTools;

    [Header("Cinematic Actions")]
    public TutorialAction actionOnDisplay = TutorialAction.None;
    public int targetSeat = 0; 
    
    [Header("Force Dialog Settings")]
    public string forcedBubbleText;
    public Color forcedBubbleColor = Color.white;

    [Header("Guide Arrow System")]
    public bool showArrow;
    public bool pointToStudent;
    public string uiButtonName;
    public float arrowAngle = 180f; 

    [Header("Time Control (Ignores Pause)")]
    public float delayBeforeShowing = 0f;
    public float autoAdvanceDuration = 0f;
}