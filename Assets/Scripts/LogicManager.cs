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

    void Start()
    {
        isGameActive = true;
        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
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
        int graduatedCount = 0;
        
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) dropoutCount++;
            else if (s.currentState == StudentState.Graduated) graduatedCount++;
        }

        currentDropouts = dropoutCount;
        
        // Le decimos a la UI que actualice el texto de las bajas
        UIManager.Instance.UpdateDropouts(currentDropouts, maxDropouts);

        if (currentDropouts >= maxDropouts)
        {
            TriggerGameOver();
            return; 
        }

        if (dropoutCount + graduatedCount >= allStudents.Count)
        {
            if (allStudents.Count < 12)
            {
                int nextAmount = allStudents.Count + 2;
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

        int graduados = 0;
        foreach (Student s in allStudents) if (s.currentState == StudentState.Graduated) graduados++;

        bool perfectSemester = (graduados == allStudents.Count);
        
        // ¡El UIManager se encarga de mostrar la pantalla final!
        UIManager.Instance.ShowEndScreen(false, perfectSemester, graduados, currentDropouts, maxDropouts, allStudents.Count);
    }

    private void TriggerGameOver()
    {
        isGameActive = false; 
        Time.timeScale = 0f;  

        int graduados = 0;
        foreach (Student s in allStudents) if (s.currentState == StudentState.Graduated) graduados++;

        // ¡Despedido! (isFired = true)
        UIManager.Instance.ShowEndScreen(true, false, graduados, currentDropouts, maxDropouts, allStudents.Count);
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
        StudentSpawner spawner = Object.FindAnyObjectByType<StudentSpawner>();
        if (spawner != null) spawner.NextRound(cantidadAlumnos);

        yield return new WaitForEndOfFrame();

        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));

        globalTimer = maxGlobalTimer;
       if (ExamManager.Instance != null)
        {
            ExamManager.Instance.ResetExamTimer();
        }
    }
}