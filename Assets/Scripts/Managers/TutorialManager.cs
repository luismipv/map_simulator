using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum TutorialTrigger 
{ 
    StartOfClass, 
    FirstExam, 
    StudentBurnout, 
    StudentFlow, 
    StudentDistracted,
    StudentDistractedByOtherStudent,
    StudentAboutToBurnout,
    StudentDragged,
    SeatChanged,
    StudentTutor
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Conexión Visual (UI)")]
    public GameObject tutorialUIContainer; // El panel que oscurece la pantalla
    public TextMeshProUGUI dialogueTextUI; // El texto del profesor/guía

    // Control Interno
    private int currentStepIndex = 0;
    private List<TutorialStepSO> currentSequence;
    private bool firstExamTriggered = false; // Para evitar que se repita en el parcial 2 y 3

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // --- CONECTANDO LOS ENCHUFES ---
    // ==========================================
    void OnEnable()
    {
        Logic.OnGameStarted += HandleGameStarted;
        ExamManager.OnExamPhaseStarted += HandleExamStarted;
    }

    void OnDisable()
    {
        Logic.OnGameStarted -= HandleGameStarted;
        ExamManager.OnExamPhaseStarted -= HandleExamStarted;
    }

    private void HandleGameStarted()
    {
        // 1. Reiniciamos el candado del examen 
        firstExamTriggered = false; 
        
        // 2. --- ¡LA CURA CONTRA LA MEMORIA DE UNITY! ---
        // Limpiamos los candados de todas las secuencias del nivel actual
        LevelData currentLevel = Logic.Instance?.currentLevel;
        if (currentLevel != null && currentLevel.tutorialSequences != null)
        {
            foreach (LevelData.TutorialSequence sequence in currentLevel.tutorialSequences)
            {
                sequence.hasTriggered = false; 
            }
        }

        // 3. El Manager grita que la clase empezó
        ReportTrigger(TutorialTrigger.StartOfClass);
    }

    private void HandleExamStarted()
    {
        // Si es la primera vez que hay un examen en este nivel...
        if (!firstExamTriggered)
        {
            ReportTrigger(TutorialTrigger.FirstExam);
            firstExamTriggered = true; // Y cerramos el candado para el resto del semestre
        }
    }

    // ==========================================
    // --- MOTOR DEL TUTORIAL ---
    // ==========================================
    public void StartSequence(List<TutorialStepSO> sequence)
    {
        // Prevención de errores si mandan una secuencia vacía
        if (sequence == null || sequence.Count == 0) return;

        currentSequence = sequence;
        currentStepIndex = 0;
        
        if (tutorialUIContainer != null) tutorialUIContainer.SetActive(true);
        
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        // Si ya nos pasamos del último paso, cerramos el tutorial
        if (currentStepIndex >= currentSequence.Count)
        {
            EndSequence();
            return;
        }

        TutorialStepSO step = currentSequence[currentStepIndex];
        
        // 1. Mostrar el texto
        if (dialogueTextUI != null) dialogueTextUI.text = step.dialogueText;

        // 2. ¿Pausar el tiempo?
        Time.timeScale = step.pausesGame ? 0f : 1f;

        // 3. Secuestrar las manos del maestro
        if (ToolManager.Instance != null)
        {
            ToolManager.Instance.SetTeacherBusy(step.lockAllTools);
        }
    }

    // Cualquier script del juego puede llamar a esta función y pasarle un "Trigger"
    public void ReportTrigger(TutorialTrigger trigger)
    {
        LevelData currentLevel = Logic.Instance?.currentLevel;
        if (currentLevel == null || !currentLevel.isTutorialLevel) return;

        // Buscamos si en la lista de este nivel hay algún tutorial configurado para este evento
        foreach (LevelData.TutorialSequence sequence in currentLevel.tutorialSequences)
        {
            if (sequence.triggerType == trigger && !sequence.hasTriggered)
            {
                if (sequence.triggerOnlyOnce) sequence.hasTriggered = true;
                
                StartSequence(sequence.steps);
                break; // Detenemos la búsqueda si ya encontramos uno
            }
        }
    }

    // ¡ESTE MÉTODO SE LO ASIGNAS AL BOTÓN "SIGUIENTE" EN TU UI!
    public void NextStep() 
    {
        if (AudioManager.Instance != null) 
            AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject);
            
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void EndSequence()
    {
        if (tutorialUIContainer != null) tutorialUIContainer.SetActive(false);
        
        Time.timeScale = 1f; // Regresamos el tiempo a la normalidad
        
        // Le devolvemos las manos al maestro
        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(false);
    }
}