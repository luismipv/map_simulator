using UnityEngine;
using System.Collections; // ¡NUEVO! Necesario para las Corrutinas (Temporizadores)
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum PlayerTool
{
    Homework,
    Nag,
    Relax,
    Tutoring
}

public class Logic : MonoBehaviour 
{
    [Header("Interacción")]
    public Student selectedStudent;
    public GameObject busyIndicatorUI;

    [Header("Gestión del Salón")]
    public List<Student> allStudents = new List<Student>(); 

    [Header("Métricas Globales: Estrés")]
    public Slider averageStressSlider;
    public TextMeshProUGUI averageStressText;

    [Header("Métricas Globales: Aprendizaje")]
    public Slider averageLearningSlider;
    public TextMeshProUGUI averageLearningText;

    [Header("Sistema de Distracción Espacial")]
    public float contagionRadius = 250f; // Ajusta este número según el tamaño de tu Canvas


    [Header("Flujo del Juego (Timer)")]
    public float maxGlobalTimer = 300f; 
    public float globalTimer = 300f;
    public Slider timerSlider;
    public TextMeshProUGUI timerText;
    [Header("Dificultad de Fin de Semestre")]
    public float maxEndSemesterMultiplier = 2f; // Al final, se estresan al doble de velocidad
    [HideInInspector] public float currentSemesterMultiplier = 1f;
    [Header("Condiciones de Derrota")]
    public int maxDropouts = 3; // Límite de alumnos dados de baja
    private int currentDropouts = 0; // Conteo actual
    public TextMeshProUGUI dropoutsText; 

    [Header("Modo Pincel")]
    public PlayerTool currentTool = PlayerTool.Homework;
    public UnityEngine.UI.Image homeworkButtonImage; // Imagen del botón Tarea
    public UnityEngine.UI.Image relaxButtonImage;    // Imagen del botón Descanso
    public UnityEngine.UI.Image tutoringButtonImage; // Imagen del botón Asesoría
    public UnityEngine.UI.Image nagButtonImage;      // Imagen del botón Regaño
    public Color colorNormal = Color.white;       // Color cuando NO está seleccionado
    public Color colorSeleccionado = Color.green;

    [Header("Pantalla Final (UI)")]
    public GameObject endGamePanel;          // El panel completo
    public GameObject gameplayContainer; // Para ocultar toda la UI de juego cuando termina
    public TMPro.TextMeshProUGUI resultTitleText; // El título (Victoria/Derrota)
    public TMPro.TextMeshProUGUI statsText; 

    [Header("Exámenes Parciales Automáticos")]
    public float partialExamInterval = 100f; // Cada cuántos segundos hay examen
    private float nextExamTimer;             // Reloj interno del examen
    public GameObject partialExamWarningUI;  // El letrero gigante de "¡EXAMEN!"
    public TextMeshProUGUI nextExamText; // Para mostrar cuánto falta para el próximo examen
    

    [Header("Efecto de Tensión para Examen")]
    public CanvasGroup tensionVignette; // El panel que se irá oscureciendo
    public float timeToStartFading = 30f; // Cuántos segundos antes del examen empieza el efecto
    public float maxTensionAlpha = 0.5f; // Qué tan opaco llegará a ser (0.5 es semi-transparente)

    //Variables privadas de control

    //private bool gameEnded = false; 
    private bool isTeacherBusy = false;
    private bool isGameActive = true;

    void Start()
    {
        isGameActive = true;
        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));
        globalTimer = maxGlobalTimer; 

        nextExamTimer = partialExamInterval; // Iniciamos el contador del examen parcial
        if(partialExamWarningUI != null) partialExamWarningUI.SetActive(false);
    }

    void Update()
    {
        if (!isGameActive) return;
        
        HandleTimer();
        HandlePartialExams();
        CheckDropouts();
        CalculateClassMetrics();
    }

    // ==========================================
    // --- LÓGICA GENERAL DEL JUEGO ---
    // ==========================================

    private void HandleTimer()
    {
        globalTimer -= Time.deltaTime;
        
        if (timerText != null) timerText.text = $"Tiempo Restante: {Mathf.RoundToInt(globalTimer)}s"; 
        if (timerSlider != null) timerSlider.value = globalTimer / maxGlobalTimer; 

        // NUEVO: Calculamos qué tan cerca estamos del final (0.0 a 1.0)
        float timePercentage = 1f - (globalTimer / maxGlobalTimer);
        
        // El multiplicador va subiendo suavemente desde 1f hasta maxEndSemesterMultiplier (ej. 2f)
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
        int activeStudents = 0; // Para no contar a los que están escondidos en el recreo
        
        foreach (Student s in allStudents)
        {
            if (s.gameObject.activeSelf) 
            {
                totalStress += s.stressLevel;
                totalLearning += s.learningLevel;
                activeStudents++;
            }
        }
        
        if (activeStudents == 0) return; // Evitar dividir entre cero si todos están en recreo

        float averageStress = totalStress / activeStudents;
        float averageLearning = totalLearning / activeStudents;

        if (averageStressText != null) averageStressText.text = $"Estrés General: {Mathf.RoundToInt(averageStress)}/100";
        if (averageStressSlider != null) averageStressSlider.value = averageStress / 100f; 

        if (averageLearningText != null) averageLearningText.text = $"Aprendizaje General: {Mathf.RoundToInt(averageLearning)}/100";
        if (averageLearningSlider != null) averageLearningSlider.value = averageLearning / 100f; 
    }
        private void CheckDropouts()
    {
        int dropoutCount = 0;
        int graduatedCount = 0;
        
        // Recorremos el salón para contar ambos estados
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.DroppedOut)
            {
                dropoutCount++;
            }
            else if (s.currentState == StudentState.Graduated)
            {
                graduatedCount++;
            }
        }

        currentDropouts = dropoutCount;

        // 1. CONDICIÓN DE DERROTA: Si llegamos al límite de bajas... ¡Game Over!
        if (currentDropouts >= maxDropouts)
        {
            TriggerGameOver();
            return; // Salimos de la función
        }

        // 2. NUEVA CONDICIÓN DE FIN DE CLASE ANTICIPADO:
        // Si ya todos se graduaron o se dieron de baja, la clase se acabó.
                // 2. CONDICIÓN DE FIN DE CLASE O AVANCE DE RONDA:
        if (dropoutCount + graduatedCount >= allStudents.Count)
        {
            // Si el salón actual se vació pero aún no llegamos a 12 alumnos... ¡Siguiente ronda!
            if (allStudents.Count < 12)
            {
                int nextAmount = allStudents.Count + 2;
                StartCoroutine(NextRoundRoutine(nextAmount));
            }
            else
            {
                // Si ya sobrevivieron la ronda de 12, entonces sí ganaron el juego completo
                Debug.Log("¡Felicidades Profesor Leyenda! Completaste todo el semestre.");
                EndGame(); 
            }
        }
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; // Pausa el juego

        if (gameplayContainer != null) gameplayContainer.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(true);

        int graduados = 0;
        foreach (Student s in allStudents) if (s.currentState == StudentState.Graduated) graduados++;

        // CASO 1: ¡Profesor Leyenda! (Todos graduados, 0 bajas)
        if (graduados == allStudents.Count)
        {
            if (resultTitleText != null) resultTitleText.text = "<color=green>¡SEMESTRE PERFECTO!</color>";
            if (statsText != null) statsText.text = "¡Increíble! Todos tus alumnos aprobaron con honores.\nTus superiores están orgullosos.";
        }
        // CASO 2: Sobreviviste (Algunos graduados, algunas bajas pero menos de 3)
        else
        {
            if (resultTitleText != null) resultTitleText.text = "<color=yellow>¡SEMESTRE CONCLUIDO!</color>";
            if (statsText != null) 
                statsText.text = $"Lograste terminar el año escolar.\n\nGraduados: {graduados} / {allStudents.Count}\nBajas: {currentDropouts}";
        }
    }

        private void TriggerGameOver()
    {
        isGameActive = false; // Detiene el reloj y lógicas si tenías esta bandera
        Time.timeScale = 0f;  // Pausa el juego por completo (física y timers)

        if (gameplayContainer != null) gameplayContainer.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(true);
        
        if (resultTitleText != null) 
            resultTitleText.text = "<color=red>¡DESPEDIDO!</color>";

        // Contamos cuántos lograste graduar antes del colapso
        int graduados = 0;
        foreach (Student s in allStudents) if (s.currentState == StudentState.Graduated) graduados++;

        if (statsText != null)
            statsText.text = $"El sindicato te reportó.\n\nGraduados: {graduados}\nBajas: {currentDropouts} / {maxDropouts}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // ¡SÚPER IMPORTANTE! Si no regresas el tiempo a 1, el juego reiniciará pausado.
        
        // Recarga la escena que está actualmente activa
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // ==========================================
    // --- NUEVAS ACCIONES GLOBALES (EL MARTILLO) ---
    // ==========================================

    [ContextMenu("GLOBAL: Chiste de Riesgo")]
    public void GlobalJokeRisk()
    {
        if (isTeacherBusy) return;
        Debug.Log("Lanzando chiste global...");
        foreach (Student s in allStudents)
        {
            // Ignoramos a los que ya se dieron de baja
            if (s.currentState == StudentState.DroppedOut) continue; 

            bool likedIt = false;

            // Reacciones basadas en la personalidad!
            switch (s.personalityData.personalityType)
            {
                case StudentPersonality.Slacker:
                    // Al flojo SIEMPRE le gustan los chistes
                    s.ModifyStressInstant(-30f);
                    likedIt = true;
                    break;
                    
                case StudentPersonality.Nerd:
                    // Al Nerd casi NUNCA le gustan, lo interrumpes de su estudio
                    if (Random.Range(0f, 100f) < 10f) { s.ModifyStressInstant(-15f); likedIt = true; }
                    else { s.ModifyStressInstant(25f); likedIt = false; }
                    break;
                    
                default:
                    // Normal y Ansioso dependen de la suerte (70% de probabilidad como tenías antes)
                    if (Random.Range(0f, 100f) <= 70f) { s.ModifyStressInstant(-25f); likedIt = true; }
                    else { s.ModifyStressInstant(20f); likedIt = false; }
                    break;
            }

            // Mostramos la carita correspondiente
            s.RequestJokeFeedback(likedIt);
        }
    }

    [ContextMenu("GLOBAL: Recreo General")]
    public void GlobalBreak()
    {
        if (isTeacherBusy) return;
        StartCoroutine(GlobalBreakRoutine());
    }

    private IEnumerator GlobalBreakRoutine()
    {
        Debug.Log("¡Recreo General! Todos desaparecen.");
        foreach (Student s in allStudents)
        {
            s.ModifyStressInstant(-40f); // Les bajamos estrés antes de irse
            s.gameObject.SetActive(false); // Apagamos su GameObject (desaparecen)
        }

        yield return new WaitForSeconds(10f); // Esperamos 10 segundos

        Debug.Log("Fin del recreo. Todos vuelven a sus lugares.");
        foreach (Student s in allStudents)
        {
            s.gameObject.SetActive(true); // Los volvemos a encender
            s.ChangeState(StudentState.Working); // Regresan a trabajar
        }
    }

    [ContextMenu("GLOBAL: Examen Sorpresa")]
    public void GlobalSurpriseExam()
    {
        if (isTeacherBusy) return;
        StartCoroutine(SurpriseExamRoutine());
    }

        private IEnumerator SurpriseExamRoutine()
    {
        Debug.Log("¡Examen Sorpresa! Reacciones según personalidad.");
        foreach (Student s in allStudents)
        {
            // Ignoramos a los dados de baja
            if (s.currentState == StudentState.DroppedOut) continue;

            s.ChangeState(StudentState.Working); // Forzamos a trabajar

            // Reacciones al Examen Sorpresa
            switch (s.personalityData.personalityType)
            {
                case StudentPersonality.Nerd:
                    s.learningMultiplier = 3.5f; // Súper concentración (Aprende muchísimo)
                    s.stressMultiplier = 1.5f;   // Se estresa, pero lo maneja
                    break;
                    
                case StudentPersonality.Slacker:
                    s.learningMultiplier = 1f;   // Le da igual, no aprende extra
                    s.stressMultiplier = 1.2f;   // Casi no se estresa por el examen
                    break;
                    
                case StudentPersonality.Anxious:
                    s.learningMultiplier = 1.5f; // Aprende un poco más...
                    s.stressMultiplier = 4f;     // ¡Pero le da PÁNICO TOTAL! (Sube rapidísimo)
                    break;
                    
                default: // Normal
                    s.learningMultiplier = 2f; 
                    s.stressMultiplier = 2f;
                    break;
            }
        }

        yield return new WaitForSeconds(8f); // El examen dura 8 segundos

        Debug.Log("Fin del examen. Todo vuelve a la normalidad.");
        foreach (Student s in allStudents)
        {
            if (s.currentState == StudentState.DroppedOut) continue;
            
            s.learningMultiplier = 1f; // Regresamos los valores a la normalidad
            s.stressMultiplier = 1f;
        }
    }
        [ContextMenu("Asesoría Privada")]
    public void GivePrivateTutoring()
    {
        // Solo si el maestro está libre y hay un alumno seleccionado
        if (selectedStudent != null && !isTeacherBusy)
        {
            StartCoroutine(PrivateTutoringRoutine(selectedStudent));
        }
    }

    private IEnumerator PrivateTutoringRoutine(Student student)
    {
        isTeacherBusy = true;
        if (busyIndicatorUI != null) busyIndicatorUI.SetActive(true);

        Debug.Log($"Iniciando asesoría privada con {student.studentName}. El maestro estará ocupado por 5s.");

        // CONFIGURACIÓN DEL RECOMPENSA:
        // Si es Ansioso, ¡aprende 10 veces más rápido! Si es otro, aprende 4 veces más rápido.
        float learningBoost = (student.personalityData.personalityType == StudentPersonality.Anxious) ? 10f : 4f;
        student.learningMultiplier = learningBoost;
        
        // Mientras el maestro le explica pacientemente, ¡su estrés BAJA en lugar de subir! (-2x)
        student.stressMultiplier = -2f; 

        // Forzamos al alumno al estado Working para que aplique los multiplicadores
        student.ChangeState(StudentState.Working);

        // Esperamos los 5 segundos de la asesoría
        yield return new WaitForSeconds(5f);

        // Al terminar, regresamos al alumno a la normalidad
        if (student.currentState != StudentState.Graduated && student.currentState != StudentState.DroppedOut)
        {
            student.learningMultiplier = 1f;
            student.stressMultiplier = 1f;
        }

        isTeacherBusy = false;
        if (busyIndicatorUI != null) busyIndicatorUI.SetActive(false);
        Debug.Log("Terminó la asesoría. El maestro vuelve a estar libre.");
    }
    // ==========================================
    // --- SISTEMA DE DISTRACCIÓN ---
    // ==========================================
    
        public void TryInfectStudent(Student source)
    {
        List<Student> infectable = new List<Student>();
        
        foreach (Student s in allStudents)
        {
            // Verificamos que no sea él mismo, y que esté trabajando o descansando
            if (s != source && (s.currentState == StudentState.Working || s.currentState == StudentState.Resting))
            {
                // Medimos la distancia física entre los dos alumnos en el Canvas
                float distance = Vector2.Distance(source.transform.position, s.transform.position);
                
                // Si está dentro del radio de contagio (es su vecino directo)
                if (distance <= contagionRadius)
                {
                    infectable.Add(s);
                }
            }
        }
        
                if (infectable.Count > 0)
        {
            // Elegimos a la "víctima" al azar de entre los vecinos
            int randomIndex = Random.Range(0, infectable.Count);
            Student target = infectable[randomIndex];

            if (Random.Range(0f, 100f) <= 40f) 
            {
                target.ChangeState(StudentState.Distracted);
                
                // Ambos muestran feedback de éxito
                source.RequestDistractionFeedback(true, target.studentName);
                target.RequestDistractionFeedback(true, source.studentName);
                
                Debug.Log($"¡El chisme pegó! {source.studentName} distrajo a su vecino {target.studentName}");
            }
            else
            {
                // El que inició el chisme se queda con las ganas, el otro lo rechaza
                //source.RequestDistractionFeedback(false);
                target.RequestDistractionFeedback(false, source.studentName);
                
                Debug.Log($"{source.studentName} intentó distraer a {target.studentName}, pero lo ignoró.");
            }
        }
    }
    // ==========================================
    // --- INTERACCIÓN INDIVIDUAL (BISTURÍ) ---
    // ==========================================
    
   

        // Conecta estas funciones a tus botones de la UI Global
    public void SelectToolHomework() { currentTool = PlayerTool.Homework; UpdateButtonVisuals(); Debug.Log("Herramienta: Tarea"); }
    public void SelectToolRelax() { currentTool = PlayerTool.Relax; UpdateButtonVisuals(); Debug.Log("Herramienta: Descanso"); }
    public void SelectToolTutoring() { currentTool = PlayerTool.Tutoring; UpdateButtonVisuals(); Debug.Log("Herramienta: Asesoría"); }
    public void SelectToolNag() { currentTool = PlayerTool.Nag; UpdateButtonVisuals(); Debug.Log("Herramienta: Regaño"); }

    private void UpdateButtonVisuals()
    {
        // Primero ponemos todos en color normal
        if (homeworkButtonImage != null) homeworkButtonImage.color = colorNormal;
        if (relaxButtonImage != null) relaxButtonImage.color = colorNormal;
        if (tutoringButtonImage != null) tutoringButtonImage.color = colorNormal;
        if (nagButtonImage != null) nagButtonImage.color = colorNormal;

        // Pintamos el que esté actualmente activo
        switch (currentTool)
        {
            case PlayerTool.Homework:
                if (homeworkButtonImage != null) homeworkButtonImage.color = colorSeleccionado;
                break;
            case PlayerTool.Relax:
                if (relaxButtonImage != null) relaxButtonImage.color = colorSeleccionado;
                break;
            case PlayerTool.Tutoring:
                if (tutoringButtonImage != null) tutoringButtonImage.color = colorSeleccionado;
                break;
            case PlayerTool.Nag:
                if (nagButtonImage != null) nagButtonImage.color = colorSeleccionado;
                break;
        }
    }
    public void ApplyToolToStudent(Student student)
    {
        // Si el maestro está dando asesoría, no puede usar herramientas
        if (isTeacherBusy) return;

        // Dependiendo de qué herramienta tienes en la mano, pasa una cosa distinta
        switch (currentTool)
        {
            case PlayerTool.Homework:
                if (student.currentState == StudentState.Resting)
                {
                    Debug.Log($"{student.studentName} está en su descanso obligatorio. ¡Déjalo respirar!");
                    return; // Salimos de la función sin hacer nada
                }
                student.ModifyStressInstant(20f);
                student.ModifyLearningInstant(10f);
                Debug.Log($"¡Le pusiste tarea a {student.studentName}!");
                break;

            case PlayerTool.Relax:
                // Lo manda a descansar
                if (student.currentState == StudentState.Resting) return; // Si ya está descansando, no hacemos nada
                if (student.currentRestCooldown > 0f)
                {
                    Debug.Log($"{student.studentName} ya descansó hace poco. ¡A trabajar!");
                    return; // Rechazamos el descanso
                }
                student.ChangeState(StudentState.Resting);
                Debug.Log($"{student.studentName} fue enviado a descansar.");
                break;

            case PlayerTool.Tutoring:
                // Inicia la corrutina de asesoría
                if (student.currentState == StudentState.Resting) return;
                StartCoroutine(PrivateTutoringRoutine(student));
                break;
            case PlayerTool.Nag:
                // Solo si el alumno está distraído, el regaño tiene efecto
                if (student.currentState == StudentState.Distracted)
                {
                    student.ModifyStressInstant(10f); 
                    student.ChangeState(StudentState.Working); // Lo regañas y lo vuelves a poner a trabajar
                    Debug.Log($"¡Regañaste a {student.studentName}!");
                }
                else if(student.currentState == StudentState.Resting)
                {
                    student.ModifyStressInstant(25f); // Regañar a un alumno que está descansando lo estresa porque lo interrumpes
                    Debug.Log($"¡{student.studentName} está descansando! Regañarlo lo estresa mucho.");
                }
                else
                {
                    student.ModifyStressInstant(20f); // Regañar a un alumno que no está distraído lo estresa más porque se siente injustamente tratado
                    Debug.Log($"Intentaste regañar a {student.studentName}, pero no estaba distraído.");
                }
                break;
        }
    }

    // ==================================================
    // --- SISTEMA DE EXÁMENES PARCIALES AUTOMÁTICOS ---
    // ==================================================  

    private void HandlePartialExams()
    {
        nextExamTimer -= Time.deltaTime;

        if (nextExamText != null) 
            nextExamText.text = $"Siguiente Parcial: {Mathf.RoundToInt(nextExamTimer)}s";
            

        // ==========================================
        // NUEVO: EFECTO DE TENSIÓN VISUAL (ALPHA)
        // ==========================================
        if (tensionVignette != null)
        {
            // Solo empezamos a oscurecer si faltan 30 segundos (o lo que hayas configurado)
            if (nextExamTimer <= timeToStartFading)
            {
                // Matemáticas mágicas: Convierte los 30s restantes en un porcentaje de 0 a 1
                float fadePercentage = 1f - (nextExamTimer / timeToStartFading);
                
                // Multiplicamos por el máximo Alpha para que no quede negro/rojo sólido
                tensionVignette.alpha = fadePercentage * maxTensionAlpha; 
            }
            else
            {
                // Si falta mucho tiempo, lo mantenemos 100% invisible
                tensionVignette.alpha = 0f; 
            }
        }
        // ==========================================

        if (nextExamTimer <= 0f)
        {
            StartCoroutine(PartialExamRoutine());
            nextExamTimer = partialExamInterval; 
        }
    }

    private IEnumerator PartialExamRoutine()
    {
        Debug.Log("¡Examen Parcial Activado!");
        
        // Prendemos el letrero de advertencia
        if (partialExamWarningUI != null) partialExamWarningUI.SetActive(true);

        // Pausamos el juego por completo
        Time.timeScale = 0f;

        // Esperamos 2 segundos en TIEMPO REAL (ignorando que el juego está pausado)
        yield return new WaitForSecondsRealtime(2f);

        // Pasamos a evaluar a cada alumno
        foreach (Student s in allStudents)
        {
            // Si ya no está en clase, lo ignoramos
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // EL CORTE: ¿Tiene más de la mitad de la barra de aprendizaje llena?
            if (s.learningLevel >= (s.maxLearning / 2f))
            {
                // Premio: Pasó el examen, siente un gran alivio
                s.ModifyStressInstant(-35f); 
                s.RequestExamFeedback(true);
                Debug.Log($"{s.studentName} pasó el parcial tranquilamente.");
            }
            else
            {
                // Castigo: Reprobó el parcial, entra en crisis
                s.ModifyStressInstant(40f);
                s.RequestExamFeedback(false); // Llamamos al feedback visual
                Debug.Log($"{s.studentName} reprobó el parcial. ¡Pánico!");
            }
        }

        

        // Apagamos el letrero
        if (partialExamWarningUI != null) partialExamWarningUI.SetActive(false);
        // Limpiamos la tensión visual para el siguiente periodo
        if (tensionVignette != null) tensionVignette.alpha = 0f;
        
        // Reanudamos el juego
        Time.timeScale = 1f;
    }

    // ==========================================
    // --- DEBUG VISUAL (GIZMOS) ---
    // ==========================================
    private void OnDrawGizmos()
    {
        // Solo dibujamos si el juego está corriendo y la lista de estudiantes existe
        if (!Application.isPlaying || allStudents == null || allStudents.Count == 0) return;

        // Configuramos el color de la línea (Naranja para coincidir con el estado Distraído)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); 

        // Recorremos el salón buscando a los chismosos
        foreach (Student student in allStudents)
        {
            // Si el alumno está activo y está distraído, dibujamos su radio
            if (student.gameObject.activeSelf && student.currentState == StudentState.Distracted)
            {
                // Dibuja una esfera de líneas tomando su posición exacta y el radio que pusiste en el Inspector
                Gizmos.DrawWireSphere(student.transform.position, contagionRadius);
            }
        }
    }

        private IEnumerator NextRoundRoutine(int cantidadAlumnos)
    {
        Debug.Log($"¡Ronda completada! Preparando salón para {cantidadAlumnos} alumnos...");
        
        // 1. Buscamos el spawner y le ordenamos reiniciar el salón con la nueva cantidad
        StudentSpawner spawner = Object.FindAnyObjectByType<StudentSpawner>();
        if (spawner != null)
        {
            spawner.NextRound(cantidadAlumnos);
        }

        // 2. Esperamos al final del frame para que Unity termine de borrar los viejos e instanciar los nuevos
        yield return new WaitForEndOfFrame();

        // 3. Volvemos a escanear el salón para actualizar nuestra lista de alumnos activos
        allStudents = new List<Student>(Object.FindObjectsByType<Student>(FindObjectsSortMode.None));

        // [Opcional] Si quieres que el temporizador global se reinicie en cada ronda, descomenta la siguiente línea:
        globalTimer = maxGlobalTimer;
        partialExamInterval = 100f; // Reiniciamos el contador del examen parcial también

        Debug.Log("¡Nueva ronda iniciada con éxito!");
    }

}