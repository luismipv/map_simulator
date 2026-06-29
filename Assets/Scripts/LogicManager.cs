using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ExamPenaltyMode 
{ 
    PanicAttack, 
    MoneyFine, 
    Snowball 
}

public class Logic : MonoBehaviour 
{
    [Header("Sistema de Exámenes (Modo Pruebas)")]
    public ExamPenaltyMode currentTestMode = ExamPenaltyMode.PanicAttack;
    public float partialLearningQuota = 100f; // La cuota para pasar el parcial
    public int currentMoney = 0;              // El dinero del Maestro
    public int moneyPerPass = 50;             // Cuánto te pagan por alumno que apruebe


    [Header("Sistema de Semestre")]
    public int currentPartial = 1;
    public int totalPartials = 3;
    

    [Header("Flujo del Juego (Timer)")]
    public float maxGlobalTimer = 300f; 
    public float globalTimer = 300f;
    public float maxEndSemesterMultiplier = 2f; 
    [HideInInspector] public float currentSemesterMultiplier = 1f;
    [HideInInspector] public int graduatedStudents = 0; 

    [Header("Condiciones de Derrota")]
    public int maxDropouts = 3; 
    private int currentDropouts = 0; 

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

    [Header("Protección Anti-Spam")]
    public float toolCooldown = 0.2f; // 0.2 segundos entre clics
    private float lastToolUsageTime = 0f;
    
    // Diccionarios y controles privados
    public Dictionary<Student, int> homeworkStreak = new Dictionary<Student, int>(); 
    private bool isGameActive = true;

    // ==========================================
    // --- INICIALIZACIÓN Y BUCLE PRINCIPAL ---
    // ==========================================

   void Start()
    {
        // 1. CONGELAMOS EL JUEGO AL INICIAR
        isGameActive = false; 
        
        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        totalStudentsThisRound = allStudents.Count;
        globalTimer = maxGlobalTimer; 

        // 2. MOSTRAMOS EL MENÚ (Y apagamos la interfaz de juego)
        UIManager.Instance.startMenuPanel.SetActive(true);
        UIManager.Instance.gameplayContainer.SetActive(false);
    }

    void Update()
    {
        if (!isGameActive) return;
        
        HandleTimer();
        CheckDropouts();
        CalculateClassMetrics();
        CheckEarlyFinish();
    }

    // ==========================================
    // --- LÓGICA DE CLASE EN TIEMPO REAL ---
    // ==========================================

    private void HandleTimer()
    {
        globalTimer -= Time.deltaTime;
        UIManager.Instance.UpdateTimer(globalTimer, maxGlobalTimer);

        // ¡NUEVO! Alimentamos tu texto amarillo y la viñeta roja directamente con el reloj global
        // (El 60f es el tiempo en el que quieres que empiece a salir la viñeta roja)
        UIManager.Instance.UpdateExamUI(globalTimer, 30f, 0.8f);

        float timePercentage = 1f - (globalTimer / maxGlobalTimer);
        currentSemesterMultiplier = Mathf.Lerp(1f, maxEndSemesterMultiplier, timePercentage);
        
        if (globalTimer <= 0f)
        {
            TriggerExamPhase();
        }
    }

    private void CalculateClassMetrics()
    {
        if (allStudents.Count == 0) return;

        float totalStress = 0f;
        float totalLearning = 0f;
        int activeStudents = 0; 
        
        foreach (Student s in allStudents)
        {
            // Solo promediamos a los que siguen en clase
            if (s.gameObject.activeSelf && s.currentState != StudentState.DroppedOut) 
            {
                totalStress += s.stressLevel;
                totalLearning += s.learningLevel;
                activeStudents++;
            }
        }
        
        if (activeStudents == 0) return; 

        float averageStress = totalStress / activeStudents;
        float averageLearning = totalLearning / activeStudents;

        UIManager.Instance.UpdateMetrics(averageStress, averageLearning);
    }

    private void CheckDropouts()
    {
        int dropoutCount = 0;
        
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) dropoutCount++;
        }

        currentDropouts = dropoutCount;
        UIManager.Instance.UpdateDropouts(currentDropouts, maxDropouts);

        // Si superamos el límite de bajas, estás despedido
        if (currentDropouts >= maxDropouts)
        {
            TriggerGameOver();
        }
    }

    private void CheckEarlyFinish()
    {
        if (!isGameActive) return;

        int finishedOrOutCount = 0;
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.Finished || s.currentState == StudentState.DroppedOut)
            {
                finishedOrOutCount++;
            }
        }

        if (finishedOrOutCount >= totalStudentsThisRound && totalStudentsThisRound > 0)
        {
            Debug.Log("¡Todos terminaron temprano! Adelantando el reloj...");
            
            int timeBonus = Mathf.RoundToInt(globalTimer);
            currentMoney += timeBonus;
            
            globalTimer = 0f; 
            
            // ¡EL FIX! Faltaba disparar el examen en este momento exacto
            TriggerExamPhase(); 
        }
    }

    // ==========================================
    // --- FASE 3: EL EXAMEN (RELÓJ DETENIDO) ---
    // ==========================================

    private void TriggerExamPhase()
    {
        isGameActive = false; 
        isTeacherBusy = true;

        Debug.Log($"--- INICIANDO EXAMEN PARCIAL {currentPartial} ---");

        int passedStudents = 0;
        int failedStudents = 0;
        int moneyEarnedThisRound = 0;
        
        // ¡NUEVO: Aquí construiremos el reporte de texto!
        string dashboardDetails = ""; 

        foreach (Student s in allStudents)
        {
            if (s == null || s.currentState == StudentState.DroppedOut) continue;

            float finalLearning = s.learningLevel;
            float currentStress = s.stressLevel;
            string studentNote = ""; // La nota especial para el dashboard

            // APLICAMOS LA LÓGICA DEL INTERRUPTOR
            switch (currentTestMode)
            {
                case ExamPenaltyMode.PanicAttack:
                    if (currentStress >= 80f)
                    {
                        finalLearning -= 20f; 
                        studentNote = "<color=red>¡Se quedó en blanco por pánico! (-20 pts)</color>";
                    }
                    else studentNote = "<color=green>Mente fría y enfocada</color>";
                    break;

                case ExamPenaltyMode.MoneyFine:
                    if (currentStress >= 80f)
                    {
                        moneyEarnedThisRound -= 25; 
                        studentNote = "<color=orange>Queja de padres en dirección (-$25)</color>";
                    }
                    else studentNote = "<color=green>Sin quejas</color>";
                    break;

                case ExamPenaltyMode.Snowball:
                    int roundedStress = Mathf.RoundToInt(currentStress);
                    
                    if (currentStress >= 80f)
                    {
                        studentNote = $"<color=#FF5555>Al borde del Burnout (Arrastra {roundedStress}% al P{currentPartial + 1})</color>";
                    }
                    else if (currentStress >= 40f)
                    {
                        studentNote = $"<color=yellow>Sobreviviendo (Arrastra {roundedStress}% al P{currentPartial + 1})</color>";
                    }
                    else
                    {
                        studentNote = $"<color=green>Mente fresca (Arrastra {roundedStress}% al P{currentPartial + 1})</color>";
                    }
                    break;
            }

            // EL VEREDICTO FINAL
            bool passedThisExam = false;
            if (finalLearning >= partialLearningQuota || s.currentState == StudentState.Finished)
            {
                passedStudents++;
                moneyEarnedThisRound += moneyPerPass;
                passedThisExam = true;
            }
            else
            {
                failedStudents++;
            }

            // AGREGAMOS AL ALUMNO AL REPORTE VISUAL
            string statusColor = passedThisExam ? "<color=green>APROBADO</color>" : "<color=red>REPROBADO</color>";
            dashboardDetails += $"• <b>{s.studentName}</b>: {Mathf.RoundToInt(finalLearning)} pts | {statusColor} | {studentNote}\n";

            // PREPARAMOS AL ALUMNO PARA EL SIGUIENTE PARCIAL
            s.learningLevel = 0f; 
            if (currentTestMode != ExamPenaltyMode.Snowball) s.stressLevel = 0f; 
            
            s.isExamMode = true; // Congelamos usando tu nueva bandera booleana
        }

        currentMoney += moneyEarnedThisRound;
        if (currentMoney < 0) currentMoney = 0; 

        // Enviamos todo a la UI, incluyendo la lista detallada y el modo actual
        UIManager.Instance.ShowExamResults(passedStudents, failedStudents, moneyEarnedThisRound, currentMoney, currentTestMode, dashboardDetails);
    }

    public void StartNextPartial()
    {
        currentPartial++;

        if (currentPartial > totalPartials)
        {
            Debug.Log("¡Semestre completado! Hora de la graduación final.");
            EndGame(); 
            return;
        }

        // 1. ESCALAMOS LA DIFICULTAD
        partialLearningQuota += 50f; 
        maxGlobalTimer -= 30f; 
        if (maxGlobalTimer < 120f) maxGlobalTimer = 120f; 

        // 2. LIMPIEZA DE BUFFS (Quitamos el aura del Tutor)
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

        // 3. REINICIO DE RELOJ Y ESTADO
        globalTimer = maxGlobalTimer;
        isGameActive = true;
        isTeacherBusy = false;

        UIManager.Instance.examResultsPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);

        Debug.Log($"--- INICIANDO PARCIAL {currentPartial} | Nueva Cuota: {partialLearningQuota} ---");
    }

    // ==========================================
    // --- FINALES DEL JUEGO (VICTORIA O DERROTA) ---
    // ==========================================

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 

        int survivedStudents = totalStudentsThisRound - currentDropouts;
        bool perfectSemester = (currentDropouts == 0);
        
        UIManager.Instance.ShowEndScreen(false, perfectSemester, survivedStudents, currentDropouts, maxDropouts, totalStudentsThisRound);
    }

    private void TriggerGameOver()
    {
        isGameActive = false; 
        Time.timeScale = 0f;  

        int survivedStudents = totalStudentsThisRound - currentDropouts;
        UIManager.Instance.ShowEndScreen(true, false, survivedStudents, currentDropouts, maxDropouts, totalStudentsThisRound);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ==========================================
    // --- HERRAMIENTAS Y CLICS ---
    // ==========================================
    
    public void SelectTool(TeacherTool newTool)
    {
        currentModularTool = newTool;
        ToolButtonUI[] allButtons = Object.FindObjectsByType<ToolButtonUI>(FindObjectsSortMode.None);
        foreach (ToolButtonUI btn in allButtons)
        {
            btn.UpdateVisualState(currentModularTool, colorNormal, colorSeleccionado);
        }
    }

    public void StartGameWithMode(int modeIndex)
    {
        // Convertimos el número (0, 1 o 2) en el modo de juego
        currentTestMode = (ExamPenaltyMode)modeIndex;

        if (spawner != null)
        {
            spawner.SpawnStudentsInSeats();
        }

        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        totalStudentsThisRound = allStudents.Count;
        
        Debug.Log($"¡Partida iniciada! Modo: {currentTestMode} | Alumnos creados: {totalStudentsThisRound}");

        // 3. APAGAMOS EL MENÚ Y PRENDEMOS EL SALÓN
        UIManager.Instance.startMenuPanel.SetActive(false);
        UIManager.Instance.gameplayContainer.SetActive(true);
        
        // 4. ¡QUE COMIENCE LA CLASE!
        isGameActive = true;
    }

    public void ApplyToolToStudent(Student student)
    {
        // Si el maestro está ocupado O no ha pasado suficiente tiempo, cancelamos el clic
        if (isTeacherBusy || currentModularTool == null || (Time.time < lastToolUsageTime + toolCooldown)) 
            return;

        currentModularTool.ApplyToolEffect(student, this);      
        
        // Marcamos el tiempo del último uso exitoso
        lastToolUsageTime = Time.time;
    }
}