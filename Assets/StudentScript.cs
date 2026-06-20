using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using UnityEngine.EventSystems;
using System.Collections;

public enum StudentState 
{
    Working,
    Flow,
    Burnout,
    Resting,
    DroppedOut,
    Distracted,
    Graduated
}

public enum StudentPersonality
{
    Normal,
    Nerd,    // Aplicado
    Slacker, // Flojo
    Anxious  // Ansioso
}

public class Student : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos del Estudiante")]
    public string studentName = "Juan Perez"; 
    public StudentState currentState = StudentState.Working; 
    public StudentPersonality personality = StudentPersonality.Normal;

    [Header("Estadísticas: Estrés")]
    public float stressLevel = 0f; 
    public float maxStress = 100f;
    public float workingStressRate = 5f;     
    public float flowStressRate = 15f;       
    public float restingRecoveryRate = 10f;  

    [Header("Estadísticas: Aprendizaje")]
    public float learningLevel = 0f;
    public float maxLearning = 100f;
    public float workingLearningRate = 2f;   
    public float flowLearningRate = 8f; 

        // Multiplicadores para eventos globales
    [HideInInspector] public float stressMultiplier = 1f;
    [HideInInspector] public float learningMultiplier = 1f;    

    [Header("Configuración de Flow")]
    public float flowDuration = 5f; 
    private float currentFlowTimer = 0f; 

    [Header("Configuración de Descanso")]
    public float mandatoryRestDuration = 4f; // Cuánto dura el descanso automático
    private float currentRestTimer = 0f;

    [Header("Restricciones de Descanso")]
    public float restCooldownDuration = 8f; // Segundos que debe esperar para OTRO descanso
    [HideInInspector] public float currentRestCooldown = 0f;

    [Header("Configuración de Burnout / Baja")]
    public float burnoutTimeLimit = 10f; // Segundos que tienes para salvarlo
    private float currentBurnoutTimer = 0f;
    public Color droppedOutColor = Color.gray; // Color cuando se dan de baja

    [Header("Configuración de Distracción")]
    public Color distractedColor = new Color(1f, 0.5f, 0f); // Naranja
    private float contagionTimer = 0f;
    public float contagionInterval = 12f; // Intenta contagiar a alguien cada 4 segundos

    [Header("UI General")]
    public GameObject buttonsPanel; 
    public Slider stressSlider; 
    public Slider learningSlider; 
    public TextMeshProUGUI stressText; 
    public TextMeshProUGUI learningText; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI personalityText;

    [Header("UI Botones")]
    public Button examButton;
    public Button hwButton;
    public Button jokeButton;
    public Button relaxButton;

    [Header("UI Mensajes")]
    public GameObject burnedOutMesssage;
    public GameObject flowMessage;

    [Header("UI de Feedback (Chiste Global)")]
    public GameObject happyFaceIcon; 
    public GameObject angryFaceIcon;
    public float jokeFeedbackDuration = 2f; 

    [Header("Visuales")]
    public SpriteRenderer spriteRenderer; 
    public Color restingColor = Color.green;
    public Color workingColor = Color.white;
    public Color flowColor = Color.cyan;
    public Color burnoutColor = Color.red;
    public Color hoverColor = Color.yellow;

    [Header("Efectos Visuales (Juice)")]
    public float burnoutWarningThreshold = 85f; // A partir de cuánto estrés empieza a vibrar
    public float shakeIntensity = 2f;           // Qué tan violenta es la vibración
    private Vector3 originalPosition;           // Para recordar dónde estaba sentado originalmente
    private bool isShaking = false;             // Bandera para saber si lo estamos moviendo
    

    // Variables Privadas
    private Logic logicManager; 
    private Color colorOriginalDeEstado; 
    private bool estaSiendoResaltado = false;

    void Start()
    {
        Canvas myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) myCanvas.worldCamera = Camera.main;
        personality = (StudentPersonality)Random.Range(0, 4);
        nameText.text = studentName; // Mostramos el nombre al inicio
        personalityText.text = $"Personalidad: {personality}"; // Mostramos la personalidad al inicio
        // Aseguramos que los íconos de feedback estén ocultos al inicio
        if (happyFaceIcon != null) happyFaceIcon.SetActive(false);
        if (angryFaceIcon != null) angryFaceIcon.SetActive(false);

        logicManager = Object.FindAnyObjectByType<Logic>();
        ApplyPersonalityTraits();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Configuración inicial de UI
        buttonsPanel.SetActive(false); 

        // Forzamos la actualización visual y de mensajes inicial
        ChangeState(currentState); 
        UpdateUI();
        originalPosition = transform.localPosition;
    }

    private void ApplyPersonalityTraits()
    {
        switch (personality)
        {
            case StudentPersonality.Normal:
                // Se queda con los valores base que pongas en el Inspector
                break;
                
            case StudentPersonality.Nerd:
                workingLearningRate *= 1.4f; // Aprende 40% más rápido
                workingStressRate *= 1.2f;   // Se estresa 20% más rápido
                break;
                
            case StudentPersonality.Slacker:
                workingLearningRate *= 0.6f; // Aprende muy lento
                workingStressRate *= 0.4f;   // Casi no se estresa
                break;
                
            case StudentPersonality.Anxious:
                workingLearningRate *= 1.1f; // Aprende un poco menos por la ansiedad
                workingStressRate *= 1.5f;     // Se estresa el DOBLE de rápido
                restingRecoveryRate *= 0.5f; // Le cuesta el doble de tiempo calmarse en recreo
                break;
        }
        float stressVariance = Random.Range(0.85f, 1.15f);
        float learningVariance = Random.Range(0.85f, 1.15f);

        workingStressRate *= stressVariance;
        workingLearningRate *= learningVariance;
    }

    void Update()
    {
        HandleStateLogic();
        CheckAutomaticTransitions();
        UpdateUI();
        if(currentState != StudentState.Resting) HandleShakeEffect(); // Solo vibramos si no están descansando, porque si ya están descansando no tiene sentido que tiemblen (están tranquilos aunque estén al borde del burnout)
        if (currentRestCooldown > 0f)
        {
            currentRestCooldown -= Time.deltaTime;
        }
    }

    // ==========================================
    // --- MÉTODOS DE ESTADO Y LÓGICA CORE ---
    // ==========================================

    public void ChangeState(StudentState newState)
    {
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) return;  
        currentState = newState;
        
        // Lógica de entrada al estado
        if (currentState == StudentState.Graduated)
        {
            Debug.Log(studentName + " ¡Aprobó y se fue a casa!");
            if (buttonsPanel != null) buttonsPanel.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (stressSlider != null) stressSlider.gameObject.SetActive(false);
            if (learningSlider != null) learningSlider.gameObject.SetActive(false);
            if (stressText != null) stressText.gameObject.SetActive(false);
            if (learningText != null) learningText.gameObject.SetActive(false);
            return; // Terminamos aquí
        }
        if (currentState == StudentState.Flow)
        {
            currentFlowTimer = flowDuration;
        }
        else if (currentState == StudentState.Burnout)
        {
            currentBurnoutTimer = burnoutTimeLimit;
            ModifyLearningInstant(-20f); // Penalización instantánea de aprendizaje al quemarse
        }
        else if (currentState == StudentState.Distracted) 
        {
            contagionTimer = contagionInterval;
        }
        else if (currentState == StudentState.Resting)
        {
            currentRestTimer = mandatoryRestDuration;
        }

        UpdateStateMessages();
        UpdateVisuals(); 
    }

    private void HandleStateLogic()
    {
        switch (currentState)
        {
            case StudentState.Resting:
                stressLevel -= restingRecoveryRate * Time.deltaTime;
                learningLevel -= (restingRecoveryRate * 0.05f) * Time.deltaTime;
                currentRestTimer -= Time.deltaTime;
                if (currentRestTimer <= 0f)
                {
                    ChangeState(StudentState.Working); // Regresa al trabajo solo
                }
                break;
            
             case StudentState.Working:
                // Leemos el multiplicador de pánico del Logic (si no existe, usamos 1)
                float panicMult = (logicManager != null) ? logicManager.currentSemesterMultiplier : 1f;
                
                // Aplicamos el multiplicador extra al estrés
                stressLevel += (workingStressRate * stressMultiplier * panicMult) * Time.deltaTime;
                learningLevel += (workingLearningRate * learningMultiplier) * Time.deltaTime; 
                break;

            case StudentState.Flow:
                float flowPanicMult = (logicManager != null) ? logicManager.currentSemesterMultiplier : 1f;
                
                stressLevel += (flowStressRate * stressMultiplier * flowPanicMult) * Time.deltaTime;
                learningLevel += (flowLearningRate * learningMultiplier) * Time.deltaTime; 
                
                currentFlowTimer -= Time.deltaTime; 
                if (currentFlowTimer <= 0f) ChangeState(StudentState.Working); 
                break;

            case StudentState.Burnout:
                learningLevel -= (flowLearningRate * 0.5f) * Time.deltaTime; 
                currentBurnoutTimer -= Time.deltaTime;
                if (currentBurnoutTimer <= 0f)
                {
                    Debug.Log(studentName + " se ha dado de baja de la materia.");
                    ChangeState(StudentState.DroppedOut);
                }
                break;
                
            case StudentState.DroppedOut:
                // El castigo definitivo: Aprendizaje a 0, Estrés a 100.
                learningLevel = 0f;
                stressLevel = maxStress;
                buttonsPanel.SetActive(false); // Aseguramos que no pueda interactuar más
                break;
            case StudentState.Distracted:
                // No aprende. El estrés baja súper lento porque está viendo el celular.
                stressLevel -= (restingRecoveryRate * 0.1f) * Time.deltaTime; 
                
                // Reloj de contagio
                contagionTimer -= Time.deltaTime;
                if (contagionTimer <= 0f)
                {
                    // Le avisa al Logic que intente contagiar a alguien
                    if (logicManager != null) logicManager.TryInfectStudent(this);
                    
                    contagionTimer = contagionInterval; // Reinicia su propio reloj
                }
                break;
                case StudentState.Graduated:
                // Bloqueamos sus números para que aporten un 100 perfecto al promedio general
                learningLevel = maxLearning;
                stressLevel = 0f;
                break;
        }

        // Mantenemos los valores en sus límites en todo momento
        stressLevel = Mathf.Clamp(stressLevel, 0f, maxStress);
        learningLevel = Mathf.Clamp(learningLevel, 0f, maxLearning);
    }

    private void CheckAutomaticTransitions()
    {
        if (learningLevel >= maxLearning && currentState != StudentState.Graduated)
        {
            ChangeState(StudentState.Graduated);
            return; // Salimos para no evaluar nada más
        }
        // 1. Condición de Burnout (Tiene máxima prioridad)
        if (stressLevel >= maxStress && currentState != StudentState.Burnout)
        {
            ChangeState(StudentState.Burnout);
            return; // Salimos de la función para no evaluar Flow si ya está quemado
        }
        if (currentState == StudentState.Working && personality == StudentPersonality.Slacker && stressLevel < 40f)
        {
            // 15% de probabilidad por segundo de sacar el celular (antes 5%)
            if (Random.value < 0.15f * Time.deltaTime) ChangeState(StudentState.Distracted);
        }
        // 2. CUALQUIERA DESCANSANDO: Si su estrés ya bajó a casi cero, se aburren.
        else if (currentState == StudentState.Resting && stressLevel <= 5f)
        {
            // 35% de probabilidad por segundo. 
            // Si los dejas sus 10 segundos enteros de descanso, es casi SEGURO que se van a distraer.
            if (Random.value < 0.35f * Time.deltaTime) ChangeState(StudentState.Distracted);
        }

        // 2. Condición de entrar a Flow (El "Punto Dulce")
        if (currentState == StudentState.Working && learningLevel > 50f && stressLevel >= 60f)
        {
            ChangeState(StudentState.Flow);
        }
        // 3. Condición de salir de Flow por estrés excedido
        else if (currentState == StudentState.Flow && stressLevel > 75f)
        {
            ChangeState(StudentState.Working);
        }
    }

    // ==========================================
    // --- MÉTODOS DE MODIFICACIÓN INSTANTÁNEA --
    // ==========================================

    public void ModifyStressInstant(float amount)
    {
        stressLevel += amount;
        stressLevel = Mathf.Clamp(stressLevel, 0f, maxStress);
        UpdateUI();
    }

    public void ModifyLearningInstant(float amount)
    {
        learningLevel += amount;
        learningLevel = Mathf.Clamp(learningLevel, 0f, maxLearning);
        UpdateUI();
    }

    // ==========================================
    // --- MÉTODOS DE UI Y VISUALES ---
    // ==========================================

    private void UpdateUI()
    {
        //nameText.text = studentName;
        stressText.text = Mathf.RoundToInt(stressLevel / maxStress * 100).ToString() + "%";
        stressSlider.value = stressLevel / maxStress; 

        learningText.text = Mathf.RoundToInt(learningLevel / maxLearning * 100).ToString() + "%";
        learningSlider.value = learningLevel / maxLearning; 
    }

    private void UpdateStateMessages()
    {
        // Apagamos ambos por defecto y prendemos solo si coincide con el estado
        if (burnedOutMesssage != null) burnedOutMesssage.SetActive(currentState == StudentState.Burnout);
        if (flowMessage != null) flowMessage.SetActive(currentState == StudentState.Flow);
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case StudentState.Resting: colorOriginalDeEstado = restingColor; break;
            case StudentState.Working: colorOriginalDeEstado = workingColor; break;
            case StudentState.Flow: colorOriginalDeEstado = flowColor; break;
            case StudentState.Burnout: colorOriginalDeEstado = burnoutColor; break;
            case StudentState.DroppedOut: colorOriginalDeEstado = droppedOutColor; break;
            case StudentState.Distracted: colorOriginalDeEstado = distractedColor; break;
        }

        if (!estaSiendoResaltado)
        {
            spriteRenderer.color = colorOriginalDeEstado;
        }
    }

    // ==========================================
    // --- NUEVA FUNCIÓN DE FEEDBACK VISUAL ---
    // ==========================================

    public void ShowJokeFeedback(bool likedIt)
    {
        // Iniciamos el temporizador interno para las caritas
        StartCoroutine(JokeFeedbackRoutine(likedIt));
    }

    private IEnumerator JokeFeedbackRoutine(bool likedIt)
    {
        // Primero, nos aseguramos de apagar ambas por si acaso
        happyFaceIcon.SetActive(false);
        angryFaceIcon.SetActive(false);

        // Encendemos la carita correcta según la probabilidad
        if (likedIt)
        {
            if (happyFaceIcon != null) happyFaceIcon.SetActive(true);
            nameText.text = $"{studentName} dice: ¡Jajaja, qué buen chiste!";
            personalityText.text = "";
        }
        else
        {
            if (angryFaceIcon != null) angryFaceIcon.SetActive(true);
            nameText.text = $"{studentName} dice: ¡Ese chiste fue malo!";
            personalityText.text = "";
        }

        // Esperamos el tiempo definido (2 segundos)
        yield return new WaitForSeconds(jokeFeedbackDuration);

        // Apagamos la carita que se haya encendido
        if (likedIt)
        {
            if (happyFaceIcon != null) happyFaceIcon.SetActive(false);
            nameText.text = studentName; // Volvemos al nombre original
            personalityText.text = $"Personalidad: {personality}";
        }
        else
        {
            if (angryFaceIcon != null) angryFaceIcon.SetActive(false);
            nameText.text = studentName; // Volvemos al nombre original
            personalityText.text = $"Personalidad: {personality}";
        }
    }
        public void ShowDistractionFeedback(bool success, string partnerName = "")
    {
        StartCoroutine(DistractionRoutine(success, partnerName));
    }

    private IEnumerator DistractionRoutine(bool success, string partnerName)
    {
        // Si el alumno ya está dado de baja o graduado, ignoramos
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) yield break;

        if (success)
        {
            nameText.text = $"<color=yellow>¡Chismeando con {partnerName}!</color>";
            personalityText.text = "🗣️ 📱";
        }
        else
        {
            nameText.text = "<color=#808080>¡Shh! Déjame trabajar.</color>";
            personalityText.text = "🛑";
        }

        yield return new WaitForSeconds(2.5f);

        // Solo regresamos los textos a la normalidad si no se han graduado/dado de baja en ese inter
        if (currentState != StudentState.DroppedOut && currentState != StudentState.Graduated)
        {
            nameText.text = studentName; 
            personalityText.text = $"Personalidad: {personality}";
        }
    }

        public void ShowExamResultFeedback(bool passed)
    {
        StartCoroutine(ExamResultRoutine(passed));
    }

        private void HandleShakeEffect()
    {
        // Evaluamos si está en peligro (arriba del umbral) pero aún no ha explotado
        if (stressLevel >= burnoutWarningThreshold && 
            currentState != StudentState.Burnout && 
            currentState != StudentState.DroppedOut && 
            currentState != StudentState.Graduated)
        {
            isShaking = true;
            // Lo movemos a su posición original + un pequeño desplazamiento aleatorio en un círculo
            transform.localPosition = originalPosition + (Vector3)(Random.insideUnitCircle * shakeIntensity);
        }
        else if (isShaking)
        {
            // Si ya lo curaste (bajó su estrés) o ya explotó, lo regresamos a su silla exactamente
            transform.localPosition = originalPosition;
            isShaking = false;
        }
    }

    //=========================================
    // --- NUEVA FUNCIÓN DE FEEDBACK DE EXÁMENES ---
    //=========================================
    private IEnumerator ExamResultRoutine(bool passed)
    {
        // Bloqueamos el input si ya no está activo
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) yield break;

        if (passed)
        {
            // Usamos Oro #FFD700
            nameText.text = "<color=#FFD700>¡Aprobado! 😎</color>";
            personalityText.text = "¡Uff, qué alivio!";
        }
        else
        {
            // Usamos Rojo Carmesí #DC143C
            nameText.text = "<color=#DC143C>¡Reprobado! 😱</color>";
            personalityText.text = "¡A estudiar más!";
        }

        // Mostramos el mensaje durante 3 segundos
        yield return new WaitForSeconds(3f);

        // Regresamos los textos a la normalidad si siguen en clase
        if (currentState != StudentState.DroppedOut && currentState != StudentState.Graduated)
        {
            nameText.text = studentName; 
            personalityText.text = $"Personalidad: {personality}";
        }
    }

    // ==========================================
    // --- INTERACCIONES DEL JUGADOR ---
    // ==========================================

        public void OnStudentClicked()
    {
        // Si ya se dio de baja o se graduó, ignoramos el clic
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) return;

        if (logicManager != null)
        {
            // En lugar de seleccionarlo, ¡le aplicamos la herramienta al instante!
            logicManager.ApplyToolToStudent(this);
        }
    }

 /*   void OnExamClicked()
    {
        if (logicManager != null)
        {
            logicManager.selectedStudent = this;
            logicManager.ApplyExam();
        }
    }

    void OnHWClicked()
    {
        if (logicManager != null)
        {
            logicManager.selectedStudent = this;
            logicManager.GiveHomework();
        }
    }

    void OnJokeClicked()
    {
        if (logicManager != null)
        {
            logicManager.selectedStudent = this;
            logicManager.GivePrivateTutoring();
        }
    }

    void OnRelaxClicked()
    {
        if (logicManager != null)
        {
            logicManager.selectedStudent = this;
            logicManager.GiveBreak();
        }
    }
*/
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (logicManager != null && logicManager.selectedStudent != null && logicManager.selectedStudent != this)
        {
            if (logicManager.selectedStudent.buttonsPanel.activeSelf) return;
        }

        if (spriteRenderer != null)
        {
            estaSiendoResaltado = true;
            spriteRenderer.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (spriteRenderer != null)
        {
            estaSiendoResaltado = false;
            spriteRenderer.color = colorOriginalDeEstado; 
        }
    }
}