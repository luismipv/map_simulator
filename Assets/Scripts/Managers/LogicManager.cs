using UnityEngine;
using System;
using System.Collections.Generic;

public class Logic : MonoBehaviour 
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
    private int _lastDisplayedSecond = -1;

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

        if (LevelState.Instance != null && LevelState.Instance.SelectedLevelData != null)
            currentLevel = LevelState.Instance.SelectedLevelData;
        
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
        CheckClassStatus();
    }

    private void HandleTimer()
    {
        if (currentLevel == null || !currentLevel.enableTimer) return;

        globalTimer -= Time.deltaTime;

        // Actualización fluida de la barra en cada frame
        if (UIManager.Instance != null && UIManager.Instance.timerSlider != null)
        {
            UIManager.Instance.timerSlider.value = globalTimer / currentMaxTimer;
        }

        // Formateo de texto (strings TMP) solo una vez por segundo
        int currentSecond = Mathf.FloorToInt(globalTimer);
        if (currentSecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = currentSecond;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer(globalTimer, currentMaxTimer);
                UIManager.Instance.UpdateExamUI(globalTimer, 30f, 0.8f);
            }
        }

        float timePercentage = 1f - (globalTimer / currentMaxTimer);
        currentSemesterMultiplier = Mathf.Lerp(1f, currentLevel.maxEndSemesterMultiplier, timePercentage);
        
        if (globalTimer <= 0f) TriggerExamPhase();
    }

    private void CheckClassStatus()
    {
        if (!isGameActive) return;

        int dropoutCount = 0;
        int finishedOrOutCount = 0;

        for (int i = 0; i < allStudents.Count; i++)
        {
            Student s = allStudents[i];
            if (s == null) continue;

            if (s.currentState == StudentState.DroppedOut)
            {
                dropoutCount++;
                finishedOrOutCount++;
            }
            else if (s.currentState == StudentState.Finished)
            {
                finishedOrOutCount++;
            }
        }

        if (currentDropouts != dropoutCount)
        {
            currentDropouts = dropoutCount;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateDropouts(currentDropouts, currentLevel.maxDropouts);
            }

            if (currentDropouts >= currentLevel.maxDropouts)
            {
                TriggerGameOver();
                return;
            }
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
        _lastDisplayedSecond = -1;
        isGameActive = true;
        
        // Liberamos al maestro
        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(false);

        UIManager.Instance.gameplayContainer.SetActive(true);
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 
        
        // --- CHEQUEO DE CADENA DE TUTORIAL ---
        if (currentLevel != null && currentLevel.isTutorialLevel && currentLevel.nextLevel != null)
        {
            // El jugador sobrevivió el nivel tutorial y hay uno siguiente en la cadena.
            CargarSiguienteNivelTutorial(currentLevel.nextLevel);
            return; // Salimos para NO mostrar la pantalla de resultados normal.
        }

        // --- FLUJO NORMAL (Si no es tutorial, o es el último tutorial) ---
        int survivedStudents = totalStudentsThisRound - currentDropouts;
        bool perfectSemester = (currentDropouts == 0);
        
        OnGameOver?.Invoke(true); 
        UIManager.Instance.ShowEndScreen(false, perfectSemester, survivedStudents, currentDropouts, currentLevel.maxDropouts, totalStudentsThisRound);
    }

    // --- CARGA UNIVERSAL DE NIVELES ---
    public void LoadSpecificLevel(LevelData levelToLoad)
    {
        if (levelToLoad == null)
        {
            Debug.LogError("Intentaste cargar un nivel, pero el LevelData es null.");
            return;
        }

        // 1. Actualizamos el "Cerebro" al nuevo nivel
        currentLevel = levelToLoad;
        
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

        // 4. Restauramos el tiempo (crucial si venimos de un GameOver o pausa)
        Time.timeScale = 1f;

        // 5. Apagamos paneles que puedan estorbar de la partida anterior
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.examResultsPanel != null) UIManager.Instance.examResultsPanel.SetActive(false);
        }

        // 6. Arrancamos el nuevo nivel
        StartGameWithMode();
    }

    // --- REFACTORIZADO PARA USAR LA NUEVA FUNCIÓN ---
    private void CargarSiguienteNivelTutorial(LevelData nextLevelData)
    {
        // Ahora simplemente reutilizamos la lógica universal
        LoadSpecificLevel(nextLevelData);
        
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
        // ¡Recibimos la lista exacta de los que acaban de nacer, sin fantasmas!
        if (spawner != null) 
        {
            allStudents = spawner.SpawnStudents(currentLevel);
        }

        totalStudentsThisRound = allStudents.Count;

        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(false);

        if (UIManager.Instance != null && UIManager.Instance.timerContainer != null)
        {
            UIManager.Instance.timerContainer.SetActive(currentLevel.enableTimer);
        }
        
        UIManager.Instance.startMenuPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject);
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StartOfClass);
        _lastDisplayedSecond = -1;
        isGameActive = true;
        
        OnGameStarted?.Invoke(); 
    }
}