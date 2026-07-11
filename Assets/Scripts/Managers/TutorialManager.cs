using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
    private Coroutine currentStepCoroutine;

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
    if (currentStepIndex >= currentSequence.Count)
    {
        EndSequence();
        return;
    }

    if (currentStepCoroutine != null) StopCoroutine(currentStepCoroutine);
    
    TutorialStepSO step = currentSequence[currentStepIndex];
    currentStepCoroutine = StartCoroutine(ProcessStepSequence(step));
}

// 2. El Motor de Tiempo (Corrutina)
private IEnumerator ProcessStepSequence(TutorialStepSO step)
{
    // DELAY
    if (step.delayBeforeShowing > 0f)
    {
        if (tutorialUIContainer != null) tutorialUIContainer.SetActive(false);
        ArrowPointer.Instance?.HideArrow();
        
        yield return new WaitForSecondsRealtime(step.delayBeforeShowing);
    }

    // ARROW SYSTEM
    if (ArrowPointer.Instance != null)
    {
        if (step.showArrow)
        {
            if (step.pointToStudent && Logic.Instance.allStudents.Count > step.targetSeat)
            {
                ArrowPointer.Instance.PointTo3D(Logic.Instance.allStudents[step.targetSeat].transform, step.arrowAngle);
            }
            else
            {
                GameObject uiButton = GameObject.Find(step.uiButtonName);
                if (uiButton != null) ArrowPointer.Instance.PointToUI(uiButton.GetComponent<RectTransform>(), step.arrowAngle);
            }
        }
        else ArrowPointer.Instance.HideArrow();
    }

    // GAME STATE & TOOLS
    Time.timeScale = step.pausesGame ? 0f : 1f;
    if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(step.lockAllTools);

    // CINEMATIC ACTION
    if (step.actionOnDisplay != TutorialAction.None) ExecuteTutorialAction(step);

    // UI VISIBILITY
    if (string.IsNullOrWhiteSpace(step.dialogueText))
    {
        if (tutorialUIContainer != null) tutorialUIContainer.SetActive(false);
    }
    else
    {
        if (tutorialUIContainer != null) tutorialUIContainer.SetActive(true);
        if (dialogueTextUI != null) dialogueTextUI.text = step.dialogueText;
    }

    // AUTO-ADVANCE DURATION
    if (step.autoAdvanceDuration > 0f)
    {
        yield return new WaitForSecondsRealtime(step.autoAdvanceDuration);
        AdvanceToNextStep(); 
    }
}

// 3. La Función para Avanzar
public void AdvanceToNextStep()
{
    currentStepIndex++;
    ShowCurrentStep();
}    private void ExecuteTutorialAction(TutorialStepSO step)
    {
        if (Logic.Instance == null || Logic.Instance.allStudents == null) return;

        if (step.targetSeat < Logic.Instance.allStudents.Count)
        {
            Student alumnoBase = Logic.Instance.allStudents[step.targetSeat];
            TutorialStudent titere = alumnoBase as TutorialStudent;

            if (titere != null)
            {
                switch (step.actionOnDisplay)
                {
                    case TutorialAction.ForceDistraction:
                        titere.ForceDistraction();
                        break;
                    case TutorialAction.ForceStress:
                        titere.ForceBurnoutWarning();
                        break;
                    case TutorialAction.ReleasePuppets:
                        foreach(Student s in Logic.Instance.allStudents)
                        {
                            if(s is TutorialStudent t) t.ReleasePuppet();
                        }
                        break;
                    case TutorialAction.ForceDialog:
                        // ¡AQUÍ ESTÁ TU NUEVO MÉTODO CONECTADO AL INSPECTOR!
                        titere.ForceDialog(step.forcedBubbleText, step.forcedBubbleColor);
                        break;
                }
            }
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