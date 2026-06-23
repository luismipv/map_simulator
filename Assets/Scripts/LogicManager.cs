using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Logic : MonoBehaviour 
{
    [Header("Interacción")]
    public Student selectedStudent;

    [Header("Gestión del Salón")]
    public List<Student> allStudents = new List<Student>(); 

    [Header("Sistema de Distracción Espacial")]
    public float contagionRadius = 250f; 

    [Header("Flujo del Juego (Timer)")]
    public float maxGlobalTimer = 300f; 
    public float globalTimer = 300f;

    [Header("Dificultad de Fin de Semestre")]
    public float maxEndSemesterMultiplier = 2f; 
    [HideInInspector] public float currentSemesterMultiplier = 1f;

    [Header("Condiciones de Derrota")]
    public int maxDropouts = 3; 
    private int currentDropouts = 0; 

    [Header("Modo Pincel Modular")]
    public TeacherTool currentModularTool;
    public Color colorNormal = Color.white;       
    public Color colorSeleccionado = Color.green;

    
    //Variables privadas de control
    public Dictionary<Student, int> homeworkStreak = new Dictionary<Student, int>(); 
    public bool isTeacherBusy = false;
    private bool isGameActive = true;
    private bool isTransitioning = false;

    // --- NUEVA MEMORIA PARA LA TRANSICIÓN ---
    [HideInInspector] public int graduatedStudents = 0; 
    [HideInInspector] public int totalStudentsThisRound = 0;

    void Start()
    {
        isGameActive = true;
        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        totalStudentsThisRound = allStudents.Count;
        globalTimer = maxGlobalTimer; 
    }

    void Update()
    {
        if (!isGameActive) return;
        
        HandleTimer();
        CheckDropouts();
        CalculateClassMetrics();
    }

    private void HandleTimer()
    {
        globalTimer -= Time.deltaTime;
        
        // ¡Le avisamos a la UI que el tiempo cambió!
        UIManager.Instance.UpdateTimer(globalTimer, maxGlobalTimer);

        float timePercentage = 1f - (globalTimer / maxGlobalTimer);
        currentSemesterMultiplier = Mathf.Lerp(1f, maxEndSemesterMultiplier, timePercentage);
        
        if (globalTimer <= 0f)
        {
            EndGame();
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
            if (s.gameObject.activeSelf) 
            {
                totalStress += s.stressLevel;
                totalLearning += s.learningLevel;
                activeStudents++;
            }
        }
        
        if (activeStudents == 0) return; 

        float averageStress = totalStress / activeStudents;
        float averageLearning = totalLearning / activeStudents;

        // ¡Le pasamos los datos a la UI!
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

        if (currentDropouts >= maxDropouts)
        {
            TriggerGameOver();
            return; 
        }

        // Si los quemados + los graduados = el tamaño de la clase original, ¡pasamos de ronda!
        if (dropoutCount + graduatedStudents >= totalStudentsThisRound && !isTransitioning)
        {
            isTransitioning = true; 

            if (totalStudentsThisRound < 12)
            {
                int nextAmount = totalStudentsThisRound + 2;
                StartCoroutine(NextRoundRoutine(nextAmount));
            }
            else
            {
                Debug.Log("¡Felicidades Profesor Leyenda! Completaste todo el semestre.");
                EndGame(); 
            }
        }
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; 

        bool perfectSemester = (graduatedStudents == totalStudentsThisRound);
        UIManager.Instance.ShowEndScreen(false, perfectSemester, graduatedStudents, currentDropouts, maxDropouts, totalStudentsThisRound);
    }

    private void TriggerGameOver()
    {
        isGameActive = false; 
        Time.timeScale = 0f;  

        UIManager.Instance.ShowEndScreen(true, false, graduatedStudents, currentDropouts, maxDropouts, totalStudentsThisRound);
    }


    public void RestartGame()
    {
        Time.timeScale = 1f; 
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    
    public void SelectTool(TeacherTool newTool)
    {
        currentModularTool = newTool;
        ToolButtonUI[] allButtons = Object.FindObjectsByType<ToolButtonUI>(FindObjectsSortMode.None);
        foreach (ToolButtonUI btn in allButtons)
        {
            btn.UpdateVisualState(currentModularTool, colorNormal, colorSeleccionado);
        }
    }

    public void ApplyToolToStudent(Student student)
    {
        if (isTeacherBusy || currentModularTool == null) return;
        currentModularTool.ApplyToolEffect(student, this);      
    }

   

        private IEnumerator NextRoundRoutine(int cantidadAlumnos)
    {
        // 1. BARRIDO DEL SALÓN: Destruimos a todos los alumnos viejos y liberamos sus sillas
        foreach (Student s in allStudents)
        {
            if (s != null)
            {
                if (s.currentSeat != null) s.currentSeat.currentStudent = null; // Liberamos la silla
                Destroy(s.gameObject); // Despedimos al alumno
            }
        }

        // Esperamos un frame para que Unity los borre de la memoria por completo
        yield return new WaitForEndOfFrame();

        // 2. Llamamos al Spawner para que traiga a la nueva generación
        StudentSpawner spawner = FindAnyObjectByType<StudentSpawner>();
        if (spawner != null) spawner.NextRound(cantidadAlumnos);

        // Esperamos otro frame para que el Spawner termine de acomodarlos
        yield return new WaitForEndOfFrame();

        // 3. Actualizamos la lista oficial de Logic con los nuevos alumnos
        allStudents = new List<Student>(FindObjectsByType<Student>(FindObjectsSortMode.None));

        totalStudentsThisRound = allStudents.Count;
        graduatedStudents = 0;
        globalTimer = maxGlobalTimer;
        
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.ResetExamTimer();
        }
        isTransitioning = false; // Quitamos el seguro para permitir futuras transiciones
    }
}