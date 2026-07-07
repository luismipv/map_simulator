using UnityEngine;
using System;
using System.Collections.Generic;

public class Logic: MonoBehaviour 
{
    public static Logic Instance { get; private set; }

    [Header("Cerebro del Nivel")]
    public LevelData currentLevel;

    [Header("Estado Actual (Lectura)")]
    public int currentPartial = 1;
    public int currentMoney = 0;
    public float globalTimer;
    public float currentSemesterMultiplier = 1f;
    private float partialLearningQuota; 
    private float currentMaxTimer;
    private bool isGameActive = true;
    private int currentDropouts = 0; 

    [Header("Gestión del Salón")]
    public StudentSpawner spawner;
    public List<Student> allStudents = new List<Student>(); 
    public Student selectedStudent;
    [HideInInspector] public int totalStudentsThisRound = 0;

    public Dictionary<Student, int> homeworkStreak = new Dictionary<Student, int>(); 

    public static event Action OnGameStarted;
    public static event Action<bool> OnGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        isGameActive = false; 
        
        if (currentLevel != null)
        {
            partialLearningQuota = currentLevel.initialLearningQuota;
            currentMaxTimer = currentLevel.maxGlobalTimer;
            globalTimer = currentMaxTimer;
            currentMoney = currentLevel.startingMoney;
        }
        else Debug.LogError("¡OJO! Falta asignar el LevelData en LogicManager.");

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

    private void TriggerExamPhase()
    {
        isGameActive = false; 
        
        // ¡DELEGAMOS EL TRABAJO PESADO AL EXAM MANAGER!
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.EvaluateClass(
                allStudents, 
                currentLevel, 
                currentPartial, 
                partialLearningQuota, 
                currentMoney, 
                OnExamFinishedCallback // Esperamos la respuesta con el dinero final
            );
        }
    }

    // Callback: El ExamManager nos devuelve el saldo final después de cobrar multas
    private void OnExamFinishedCallback(int updatedMoney)
    {
        currentMoney = updatedMoney;
    }

    public void StartNextPartial()
    {
        currentPartial++;

        if (currentPartial > currentLevel.totalPartials)
        {
            EndGame(); 
            return;
        }

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
        
        // Liberamos al maestro
        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(false);

        UIManager.Instance.examResultsPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        bool perfectSemester = (currentDropouts == 0);
        
        OnGameOver?.Invoke(true); 
        UIManager.Instance.ShowEndScreen(false, perfectSemester, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    private void TriggerGameOver()
    {
        isGameActive = false; 
        Time.timeScale = 0f;  
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        
        OnGameOver?.Invoke(false); 
        UIManager.Instance.ShowEndScreen(true, false, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void StartGameWithMode() 
    {
        if (spawner != null) spawner.SpawnStudents(currentLevel);

        allStudents = new List<Student>(UnityEngine.Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        totalStudentsThisRound = allStudents.Count;
        
        UIManager.Instance.startMenuPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
        isGameActive = true;
        
        OnGameStarted?.Invoke(); 
    }
}