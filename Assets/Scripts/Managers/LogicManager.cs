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
        if (!currentLevel.enableTimer) return;

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
        // 1. ¡NUEVO!: APAGAMOS EL PANEL ANTES QUE NADA PARA QUE NO ESTORBE
        if (UIManager.Instance != null && UIManager.Instance.examResultsPanel != null)
        {
            UIManager.Instance.examResultsPanel.SetActive(false);
        }

        currentPartial++;

        // 2. Revisamos si ya terminamos el nivel
        if (currentPartial > currentLevel.totalPartials)
        {
            EndGame(); 
            return; // Como ya apagamos el panel arriba, este return es 100% seguro
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

        UIManager.Instance.gameplayContainer.SetActive(true);
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 
        
        // --- ¡NUEVO!: CHEQUEO DE CADENA DE TUTORIAL ---
        if (currentLevel != null && currentLevel.isTutorialLevel && currentLevel.nextLevel != null)
        {
            // El jugador sobrevivió el nivel tutorial y hay uno siguiente en la cadena.
            // Cargamos el siguiente nivel.
            CargarSiguienteNivelTutorial(currentLevel.nextLevel);
            return; // Salimos para NO mostrar la pantalla de resultados normal.
        }

        // --- FLUJO NORMAL (Si no es tutorial, o es el último tutorial) ---
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        bool perfectSemester = (currentDropouts == 0);
        
        OnGameOver?.Invoke(true); 
        UIManager.Instance.ShowEndScreen(false, perfectSemester, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    // --- NUEVA FUNCIÓN PARA GESTIONAR EL SALTO ---
    private void CargarSiguienteNivelTutorial(LevelData nextLevelData)
    {
        // 1. Actualizamos el "Cerebro" al nuevo nivel
        currentLevel = nextLevelData;
        
        // 2. Reiniciamos los contadores internos
        currentPartial = 1;
        partialLearningQuota = currentLevel.initialLearningQuota;
        currentMaxTimer = currentLevel.maxGlobalTimer;
        globalTimer = currentMaxTimer;
        currentMoney = currentLevel.startingMoney;
        currentDropouts = 0;
        
        // 3. Limpiamos la escena de alumnos viejos
        foreach (Student s in allStudents)
        {
            if (s != null) Destroy(s.gameObject);
        }
        allStudents.Clear();
        homeworkStreak.Clear();

        // 4. Devolvemos el tiempo a la normalidad
        Time.timeScale = 1f;

        // 5. Arrancamos el nuevo nivel como si acabáramos de darle a "Start"
        StartGameWithMode();
        
        // Opcional: Podrías reproducir un sonido de "Nivel Completado" o poner un fundido a negro aquí.
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

        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(false);

        // --- ¡EL INTERRUPTOR DEL RELOJ! ---
        if (UIManager.Instance != null && UIManager.Instance.timerContainer != null)
        {
            UIManager.Instance.timerContainer.SetActive(currentLevel.enableTimer);
        }
        // ----------------------------------
        
        UIManager.Instance.startMenuPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject);
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StartOfClass);
        isGameActive = true;
        
        OnGameStarted?.Invoke(); 
    }
}