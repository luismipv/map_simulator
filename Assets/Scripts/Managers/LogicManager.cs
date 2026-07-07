using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ExamPenaltyMode { PanicAttack, MoneyFine, Snowball }

public class Logic : MonoBehaviour 
{
    public static Logic Instance { get; private set; } // PATRÓN SINGLETON

    [Header("Cerebro del Nivel")]
    public LevelData currentLevel; // Arrastra tu Scriptable Object aquí

    [Header("Estado Actual (Lectura)")]
    public int currentPartial = 1;
    public int currentMoney = 0;
    public float globalTimer;
    public float currentSemesterMultiplier = 1f;
    public int graduatedStudents = 0; 
    private int currentDropouts = 0; 
    private float partialLearningQuota; 
    private float currentMaxTimer;
    private bool isGameActive = true;

    [Header("Gestión del Salón")]
    public StudentSpawner spawner;
    public List<Student> allStudents = new List<Student>(); 
    public Student selectedStudent;
    [HideInInspector] public int totalStudentsThisRound = 0;

    [Header("Herramientas del Maestro")]
    public TeacherTool currentModularTool;
    public Color colorNormal = Color.white;       
    public Color colorSeleccionado = Color.green;
    public bool isTeacherBusy = false;
    public float toolCooldown = 0.2f; 
    private float lastToolUsageTime = 0f;
    
    public Dictionary<Student, int> homeworkStreak = new Dictionary<Student, int>(); 

    // ==========================================
    // --- LOS ENCHUFES PARA EL TUTORIAL (EVENTS) ---
    // ==========================================
    public static event Action OnGameStarted;
    public static event Action OnExamPhaseStarted;
    public static event Action<bool> OnGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        isGameActive = false; 
        
        // 1. CARGAMOS LAS REGLAS DESDE EL SCRIPTABLE OBJECT
        if (currentLevel != null)
        {
            partialLearningQuota = currentLevel.initialLearningQuota;
            currentMaxTimer = currentLevel.maxGlobalTimer;
            globalTimer = currentMaxTimer;
            currentMoney = currentLevel.startingMoney;
        }
        else Debug.LogError("¡OJO! Falta asignar el LevelData en Logic.");

        UIManager.Instance.startMenuPanel.SetActive(true);
        UIManager.Instance.gameplayContainer.SetActive(false);
    }

    void Update()
    {
        if (!isGameActive) return;
        
        HandleTimer();
        CheckDropouts();
        CheckEarlyFinish();
    }

    // ==========================================
    // --- LÓGICA DE CLASE ---
    // ==========================================

    private void HandleTimer()
    {
        globalTimer -= Time.deltaTime;
        UIManager.Instance.UpdateTimer(globalTimer, currentMaxTimer);
        UIManager.Instance.UpdateExamUI(globalTimer, 30f, 0.8f);

        float timePercentage = 1f - (globalTimer / currentMaxTimer);
        currentSemesterMultiplier = Mathf.Lerp(1f, currentLevel.maxEndSemesterMultiplier, timePercentage);
        
        if (globalTimer <= 0f) TriggerExamPhase();
    }

    private void CheckDropouts()
    {
        int dropoutCount = 0;
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) dropoutCount++;
        }

        currentDropouts = dropoutCount;
        UIManager.Instance.UpdateDropouts(currentDropouts, currentLevel.maxDropouts);

        if (currentDropouts >= currentLevel.maxDropouts) TriggerGameOver();
    }

    private void CheckEarlyFinish()
    {
        if (!isGameActive) return;
        int finishedOrOutCount = 0;
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.Finished || s.currentState == StudentState.DroppedOut)
                finishedOrOutCount++;
        }

        if (finishedOrOutCount >= totalStudentsThisRound && totalStudentsThisRound > 0)
        {
            int timeBonus = Mathf.RoundToInt(globalTimer);
            currentMoney += timeBonus;
            globalTimer = 0f; 
            TriggerExamPhase(); 
        }
    }

    // ==========================================
    // --- FASE 3: EL EXAMEN ---
    // ==========================================

    private void TriggerExamPhase()
    {
        isGameActive = false; 
        isTeacherBusy = true;
        
        OnExamPhaseStarted?.Invoke(); // Le avisa al Tutorial que empezó el examen

        int passedStudents = 0;
        int failedStudents = 0;
        int moneyEarnedThisRound = 0;
        List<StudentEvalData> studentsToAnimate = new List<StudentEvalData>();

        for (int i = allStudents.Count - 1; i >= 0; i--)
        {
            Student s = allStudents[i];
            if (s == null || s.currentState == StudentState.DroppedOut) continue;

            float finalLearning = s.learningLevel;
            float currentStress = s.stressLevel;
            bool isGraduatedThisRound = false;
            
            // LA REGLA UNIVERSAL: ATAQUE DE PÁNICO
            ExamPenaltyMode modeApplied = ExamPenaltyMode.PanicAttack; 

            if (currentStress >= 80f)
            {
                finalLearning -= 20f; // El castigo universal
                
                // Mutador extra de nivel
                if (currentLevel.enableMoneyFines) 
                {
                    moneyEarnedThisRound -= 25; 
                    modeApplied = ExamPenaltyMode.MoneyFine;
                }
            }

            if (finalLearning >= partialLearningQuota || s.currentState == StudentState.Finished)
            {
                passedStudents++;
                moneyEarnedThisRound += currentLevel.moneyPerPass;

                if (currentPartial >= currentLevel.totalPartials)
                {
                    isGraduatedThisRound = true;
                    s.ChangeState(StudentState.Graduated);
                }
            }
            else failedStudents++;

            studentsToAnimate.Add(new StudentEvalData {
                studentName = s.studentName,
                rawLearning = s.learningLevel, 
                rawStress = s.stressLevel,
                penaltyMode = modeApplied, // Enviamos el modo a la UI
                isGraduated = isGraduatedThisRound
            });

            s.learningLevel = 0f; 
            s.stressLevel = 0f; 
            s.isExamMode = true; 
        }

        currentMoney += moneyEarnedThisRound;
        if (currentMoney < 0) currentMoney = 0; 

        StartCoroutine(ShowResultsWithDelay(passedStudents, failedStudents, moneyEarnedThisRound, currentMoney, ExamPenaltyMode.PanicAttack, studentsToAnimate));
    }

    private IEnumerator ShowResultsWithDelay(int passed, int failed, int money, int totalMoney, ExamPenaltyMode mode, List<StudentEvalData> evalData)
    {
        float delayTime = (currentPartial >= currentLevel.totalPartials) ? 2.5f : 1.0f;
        yield return new WaitForSeconds(delayTime);
        UIManager.Instance.ShowExamResults(passed, failed, money, totalMoney, mode, "");
        UIManager.Instance.evaluationScreen.ShowAllResults(evalData, Mathf.RoundToInt(partialLearningQuota));
    }

    public void StartNextPartial()
    {
        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject);
        currentPartial++;

        if (currentPartial > currentLevel.totalPartials)
        {
            EndGame(); 
            return;
        }

        // Usamos los datos del LevelData para escalar
        partialLearningQuota += currentLevel.quotaIncreasePerPartial; 
        currentMaxTimer -= currentLevel.timeReductionPerPartial; 
        if (currentMaxTimer < currentLevel.minGlobalTimer) currentMaxTimer = currentLevel.minGlobalTimer; 

        for (int i = allStudents.Count - 1; i >= 0; i--)
        {
            if (allStudents[i] != null && allStudents[i].currentState == StudentState.DroppedOut)
            {
                allStudents[i].gameObject.SetActive(false); 
                allStudents.RemoveAt(i); 
            }
        }

        foreach (Student s in allStudents)
        {
            s.isExamMode = false;
            s.ChangeState(StudentState.Working);
            if (s != null && s.currentState != StudentState.DroppedOut)
            {
                s.activeLearningBuffs.Clear();
                s.activeStressBuffs.Clear();
            }
        }

        globalTimer = currentMaxTimer;
        isGameActive = true;
        isTeacherBusy = false;

        UIManager.Instance.examResultsPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
    }

    // ==========================================
    // --- FINALES Y ARRANQUE ---
    // ==========================================

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        bool perfectSemester = (currentDropouts == 0);
        
        OnGameOver?.Invoke(true); // Le avisa al tutorial/logros que ganaste
        UIManager.Instance.ShowEndScreen(false, perfectSemester, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    private void TriggerGameOver()
    {
        isGameActive = false; 
        Time.timeScale = 0f;  
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        
        OnGameOver?.Invoke(false); // Le avisa que perdiste
        UIManager.Instance.ShowEndScreen(true, false, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // Ya no recibe modo, porque las reglas las dicta el LevelData
    public void StartGameWithMode() 
    {
        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject);
        if (spawner != null) spawner.SpawnStudentsInSeats();

        allStudents = new List<Student>(UnityEngine.Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        totalStudentsThisRound = allStudents.Count;
        
        UIManager.Instance.startMenuPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
        isGameActive = true;
        
        OnGameStarted?.Invoke(); // El Tutorial entra en acción aquí
    }

    // ==========================================
    // --- HERRAMIENTAS Y CLICS ---
    // ==========================================
    
    public void SelectTool(TeacherTool newTool)
    {
        currentModularTool = newTool;
        ToolButtonUI[] allButtons = UnityEngine.Object.FindObjectsByType<ToolButtonUI>(FindObjectsSortMode.None);
        foreach (ToolButtonUI btn in allButtons)
            btn.UpdateVisualState(currentModularTool, colorNormal, colorSeleccionado);
    }

    public void ApplyToolToStudent(Student student)
    {
        if (isTeacherBusy || currentModularTool == null || (Time.time < lastToolUsageTime + toolCooldown)) 
            return;

        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject); 
        AudioManager.Instance.PostEvent("UI_Select", this.gameObject); 
        
        currentModularTool.ApplyToolEffect(student, this); 
        lastToolUsageTime = Time.time;
    }
}

[System.Serializable]
public struct StudentEvalData
{
    public string studentName;
    public float rawLearning;
    public float rawStress;
    public ExamPenaltyMode penaltyMode;
    public bool isGraduated;
}