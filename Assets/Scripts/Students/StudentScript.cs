using UnityEngine;
using UnityEngine.EventSystems;
using System; 
using System.Collections.Generic; 

public enum StudentState { Working, Flow, Burnout, Resting, DroppedOut, Distracted, Finished, Graduated }
public enum StudentPersonality { Normal, Nerd, Slacker, Anxious, Bully, Cool }

// Catálogo oficial de identificadores para modificadores (Inmune a errores de dedo y localización)
public enum ModifierID 
{ 
    Personalidad, 
    Entorno, 
    Sinergia, 
    Panico, 
    Tutor, 
    FaltaPoco,
    Tool_Tutoring,
    GlobalTool_Exam,
    Tool_Nag
}

public class Student : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    [Header("Datos del Estudiante")]
    public string studentName = "Juan Perez"; 
    public StudentState currentState = StudentState.Working; 
    public StudentPersonalitySO personalityData;

    [Header("Sistema de Asientos")]
    public Seat currentSeat;
    protected Vector3 originalPosition; 

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

    [Header("Tiempos")]
    public float flowDuration = 5f; 
    private float currentFlowTimer = 0f; 
    public float mandatoryRestDuration = 4f; 
    private float currentRestTimer = 0f;
    public float burnoutTimeLimit = 10f; 
    private float currentBurnoutTimer = 0f;
    public float contagionInterval = 12f; 
    private float contagionTimer = 0f;
    public float restCooldownDuration = 8f; 
    [HideInInspector] public float currentRestCooldown = 0f;
    [HideInInspector] public bool isExamMode = false;

    [Header("Efectos Visuales")]
    public GameObject graduationVFXPrefab;
    private Logic logicManager; 

    // --- VARIABLES DE CACHÉ Y OPTIMIZACIÓN ---
    private float _cachedLearningMultiplier = 1f;
    private float _cachedStressMultiplier = 1f;
    private Camera _mainCamera;
    private float _lastReportedStress = -1f;
    private float _lastReportedLearning = -1f;
    private float _lastSemesterMultiplier = -1f;

    // --- SISTEMA DE DICCIONARIOS SEGUROS (LLAVE: ENUM) ---
    public Dictionary<ModifierID, float> activeLearningBuffs = new Dictionary<ModifierID, float>();
    public Dictionary<ModifierID, float> activeStressBuffs = new Dictionary<ModifierID, float>();

    // ==========================================
    // --- EVENTOS (MEGÁFONOS) ---
    // ==========================================
    public event Action<float, float> OnStatsUpdated; 
    public event Action<StudentState> OnStateChanged; 
    public event Action<string, Color> OnFloatingTextRequested;    
    public event Action<string,Color> OnBubbleTextRequested;
    public event Action<bool> OnHoverChanged; 
    public event Action<bool> OnJokeFeedbackEvent; 
    public event Action OnModifiersChanged; // NUEVO EVENTO PARA OPTIMIZAR UI

    protected virtual void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Start()
    {
        logicManager = FindAnyObjectByType<Logic>();

        // Registramos los rasgos nativos en los diccionarios usando el Enum seguro
        if (personalityData != null)
        {
            AddLearningModifier(ModifierID.Personalidad, personalityData.learningRateMod);
            AddStressModifier(ModifierID.Personalidad, personalityData.stressRateMod);
            
            // Modificación directa de descanso (no requiere apilamiento dinámico)
            restingRecoveryRate *= personalityData.recoveryRateMod;
        }

        // Variabilidad individual de cada alumno
        float stressVariance = UnityEngine.Random.Range(0.85f, 1.15f);
        float learningVariance = UnityEngine.Random.Range(0.85f, 1.15f);
        workingStressRate *= stressVariance;
        workingLearningRate *= learningVariance;

        RecalculateMultipliers();
        ChangeState(currentState); 

        _lastReportedStress = stressLevel;
        _lastReportedLearning = learningLevel;
        OnStatsUpdated?.Invoke(stressLevel, learningLevel); 
    }

    void Update()
    {
        HandleStateLogic();
        CheckAutomaticTransitions();
        
        if (currentRestCooldown > 0f) currentRestCooldown -= Time.deltaTime;

        // Solo notificar a la UI cuando los valores hayan cambiado sensiblemente (> 0.05f)
        if (Mathf.Abs(stressLevel - _lastReportedStress) > 0.05f || Mathf.Abs(learningLevel - _lastReportedLearning) > 0.05f)
        {
            _lastReportedStress = stressLevel;
            _lastReportedLearning = learningLevel;
            OnStatsUpdated?.Invoke(stressLevel, learningLevel);
        }
    }

    public virtual void ChangeState(StudentState newState)
    {
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated || currentState == StudentState.Finished) return;  
        
        currentState = newState;
        
        // 1. MATAMOS EL SONIDO GLOBALMENTE AL CAMBIAR DE ESTADO
        AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);
        
        // EFECTO CONTAGIO DEL COOL
        if (currentState == StudentState.Flow && personalityData != null && personalityData.personalityType == StudentPersonality.Cool)
        {
            if (SpatialManager.Instance != null && SpatialManager.Instance.neighborGraph.ContainsKey(this))
            {
                foreach (Student neighbor in SpatialManager.Instance.neighborGraph[this])
                {
                    if (neighbor.currentState == StudentState.Working)
                    {
                        neighbor.ChangeState(StudentState.Flow);
                        neighbor.ShowFloatingText("¡Contagiado 😎!", Color.cyan);
                    }
                }
            }
        }

        // CONTROL DE ESTADOS AL CAMBIAR
        if (currentState == StudentState.Graduated)
        {
            ShowBubble("¡Se logró!", Color.yellow);
            AudioManager.Instance.StopEvent("Student_Flow",this.gameObject);
            AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);
            TriggerGraduation();
        }
        else if (currentState == StudentState.Finished)
        {
            ShowBubble("¡Listo! ¿Quién necesita ayuda?", Color.yellow);
            AudioManager.Instance.PostEvent("Student_Finished", this.gameObject); 
            TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentTutor);
            AudioManager.Instance.StopEvent("Student_Flow",this.gameObject);
            AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);

            learningLevel = maxLearning; 
            RemoveLearningModifier(ModifierID.Panico);
        }
        else if (currentState == StudentState.Flow) 
        {
            currentFlowTimer = flowDuration;
            TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentFlow);
            AudioManager.Instance.PostEvent("Student_Flow", this.gameObject);
        }
        else if (currentState == StudentState.Burnout)
        {
            currentBurnoutTimer = burnoutTimeLimit;
            ModifyLearningInstant(-20f); 
            AudioManager.Instance.StopEvent("Student_Flow", this.gameObject);
            AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);
            AudioManager.Instance.PostEvent("Student_BurnedOut", this.gameObject); 
            TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentBurnout);
        }
        else if (currentState == StudentState.Distracted) 
        {
            contagionTimer = contagionInterval;
            AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);
            RemoveLearningModifier(ModifierID.Panico);
            TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentDistracted);
        }
        else if (currentState == StudentState.Resting) 
        {
            AudioManager.Instance.StopEvent("Student_Flow", this.gameObject);
            AudioManager.Instance.StopEvent("Student_About_To_BurnOut", this.gameObject);
            AudioManager.Instance.PostEvent("Student_Resting", this.gameObject);
            currentRestTimer = mandatoryRestDuration; 
            RemoveLearningModifier(ModifierID.Panico);
        }

        OnStateChanged?.Invoke(currentState); 
    }

    private void TriggerGraduation()
    {
        if (graduationVFXPrefab != null)
        {
            Instantiate(graduationVFXPrefab, transform.position, Quaternion.identity);
            ShowFloatingText("¡Graduado!", Color.gold);
        }

        if (currentSeat != null) currentSeat.currentStudent = null;

        if (logicManager != null)
        {
            if (logicManager.allStudents.Contains(this)) logicManager.allStudents.Remove(this);
        }

        Destroy(gameObject);
    }

    protected virtual void HandleStateLogic()
    {
        if (isExamMode) return;

        // Inyección del multiplicador de fin de semestre directo al Enum seguro (Solo si cambió)
        if (logicManager != null && !Mathf.Approximately(_lastSemesterMultiplier, logicManager.currentSemesterMultiplier))
        {
            _lastSemesterMultiplier = logicManager.currentSemesterMultiplier;
            AddStressModifier(ModifierID.FaltaPoco, _lastSemesterMultiplier);
        }

        float pacingMultiplier = 1f;
        if (logicManager != null && logicManager.currentLevel != null)
        {
            pacingMultiplier = logicManager.currentLevel.learningSpeedMultiplier;
        }

        switch (currentState)
        {
            case StudentState.Resting:
                stressLevel -= restingRecoveryRate * pacingMultiplier * Time.deltaTime;
                learningLevel -= (restingRecoveryRate * 0.05f) * Time.deltaTime;
                currentRestTimer -= Time.deltaTime;
                if (currentRestTimer <= 0f || stressLevel <= 0f) ChangeState(StudentState.Working);
                break;
            
            case StudentState.Working:
                stressLevel += (workingStressRate * GetTotalStressMultiplier() * pacingMultiplier) * Time.deltaTime;
                learningLevel += (workingLearningRate * GetTotalLearningMultiplier() * pacingMultiplier) * Time.deltaTime; 
                break;

            case StudentState.Flow:
                stressLevel += (flowStressRate * GetTotalStressMultiplier() * pacingMultiplier) * Time.deltaTime;
                learningLevel += (flowLearningRate * GetTotalLearningMultiplier() * pacingMultiplier) * Time.deltaTime; 
                break;

            case StudentState.Burnout:
                learningLevel -= (flowLearningRate * 0.5f) * pacingMultiplier * Time.deltaTime; 
                currentBurnoutTimer -= Time.deltaTime;
                if (currentBurnoutTimer <= 0f) ChangeState(StudentState.DroppedOut);
                break;
                
            case StudentState.DroppedOut:
                learningLevel = 0f;
                stressLevel = maxStress;
                break;

            case StudentState.Distracted:
                stressLevel -= (restingRecoveryRate * 0.1f) * pacingMultiplier * Time.deltaTime; 
                contagionTimer -= Time.deltaTime;
                if (contagionTimer <= 0f)
                {
                    if (DistractionManager.Instance != null) DistractionManager.Instance.TryInfectStudent(this);
                    contagionTimer = contagionInterval; 
                }
                break;

            case StudentState.Graduated:
                learningLevel = maxLearning;
                stressLevel = 0f;
                break;

            case StudentState.Finished:
                learningLevel = maxLearning;
                stressLevel -= (restingRecoveryRate * 0.5f) * Time.deltaTime;
                break;
        }

        stressLevel = Mathf.Clamp(stressLevel, 0f, maxStress);
        learningLevel = Mathf.Clamp(learningLevel, 0f, maxLearning);
    }

    protected virtual void CheckAutomaticTransitions()
    {
        if (learningLevel >= maxLearning && currentState != StudentState.Finished && currentState != StudentState.Graduated) 
        { 
            ChangeState(StudentState.Finished); 
            return; 
        }
        if (stressLevel >= maxStress && currentState != StudentState.Burnout) { ChangeState(StudentState.Burnout); return; }
        
        bool distraccionesPermitidas = true;
        if (logicManager != null && logicManager.currentLevel != null)
        {
            distraccionesPermitidas = logicManager.currentLevel.enableDistractions;
        }

        if (personalityData != null)
        {
            personalityData.EvaluateSpecialBehaviors(this, distraccionesPermitidas);
        }

        if (currentState == StudentState.Resting && stressLevel <= 5f && distraccionesPermitidas)
        {
            float chanceBase = (personalityData != null) ? personalityData.distractionProbability : 5f;
            float probabilidadConvertida = chanceBase / 100f; 

            if (UnityEngine.Random.value < probabilidadConvertida * Time.deltaTime)
            {
                ChangeState(StudentState.Distracted);
                if (AudioManager.Instance != null) AudioManager.Instance.PostEvent("Student_Distracted", this.gameObject); 
                ShowBubble("Ya me aburrí...", Color.orange);
            } 
        }

        if (currentState == StudentState.Working && learningLevel > 50f && stressLevel >= 40f && stressLevel < 75f) 
        {
            ChangeState(StudentState.Flow);
        }
    }

    // --- INTERFAZ PÚBLICA OPTIMIZADA PARA UI Y DESEMPENO ---
    private void RecalculateMultipliers()
    {
        float learningMult = 1f;
        foreach (float val in activeLearningBuffs.Values) learningMult *= val;
        _cachedLearningMultiplier = learningMult;

        float stressMult = 1f;
        foreach (float val in activeStressBuffs.Values) stressMult *= val;
        _cachedStressMultiplier = stressMult;

        OnModifiersChanged?.Invoke();
    }

    public void AddLearningModifier(ModifierID id, float multiplier)
    {
        if (activeLearningBuffs.TryGetValue(id, out float existingValue) && Mathf.Approximately(existingValue, multiplier))
            return; 

        activeLearningBuffs[id] = multiplier;
        RecalculateMultipliers(); 
    }

    public void RemoveLearningModifier(ModifierID id)
    {
        if (activeLearningBuffs.Remove(id)) 
        {
            RecalculateMultipliers(); 
        }
    }

    public void AddStressModifier(ModifierID id, float multiplier)
    {
        if (activeStressBuffs.TryGetValue(id, out float existingValue) && Mathf.Approximately(existingValue, multiplier))
            return; 

        activeStressBuffs[id] = multiplier;
        RecalculateMultipliers(); 
    }

    public void RemoveStressModifier(ModifierID id)
    {
        if (activeStressBuffs.Remove(id)) 
        {
            RecalculateMultipliers(); 
        }
    }

    public void ModifyStressInstant(float amount) 
    { 
        stressLevel = Mathf.Clamp(stressLevel + amount, 0f, maxStress);
        if(amount > 0) ShowFloatingText(" +" + amount + "💢", Color.red);
        else ShowFloatingText(" " + amount + "💢", Color.green);
    }

    public void ModifyLearningInstant(float amount) 
    { 
        learningLevel = Mathf.Clamp(learningLevel + amount, 0f, maxLearning);
        if(amount > 0) ShowFloatingText(" +" + amount +"🧠", Color.green); 
        else ShowFloatingText(" " + amount +"🧠", Color.red);
    }

    public void ModifyBothStatsInstant(float stressAmount, float learningAmount)
    {
        stressLevel = Mathf.Clamp(stressLevel + stressAmount, 0f, maxStress);
        learningLevel = Mathf.Clamp(learningLevel + learningAmount, 0f, maxLearning);

        string combinedText = "";

        if (learningAmount > 0) combinedText += $"<color=#00FF00>+{learningAmount}🧠 </color>\n";
        else if (learningAmount < 0) combinedText += $"<color=#FF0000>{learningAmount}🧠 </color>\n";

        if (stressAmount > 0) combinedText += $"<color=#FF0000>+{stressAmount}💢 </color>\n";
        else if (stressAmount < 0) combinedText += $"<color=#00FF00>{stressAmount}💢 </color>\n";

        if (!string.IsNullOrEmpty(combinedText))
        {
            ShowFloatingText(combinedText, Color.white); 
        }
    }

    public void RequestJokeFeedback(bool likedIt) { OnJokeFeedbackEvent?.Invoke(likedIt); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (logicManager != null && logicManager.selectedStudent != null && logicManager.selectedStudent != this) return;
        OnHoverChanged?.Invoke(true);
    }

    public void ShowFloatingText(string text, Color color) { OnFloatingTextRequested?.Invoke(text, color); }
    public void OnPointerExit(PointerEventData eventData) { OnHoverChanged?.Invoke(false); }
    public void ShowBubble(string message, Color color) { OnBubbleTextRequested?.Invoke(message, color); }
    
    public void OnStudentClicked()
    {
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) return;
        
        if (ToolManager.Instance != null && ToolManager.Instance.currentModularTool != null)
        {
            ToolManager.Instance.ApplyToolToStudent(this);
        }

        if (StudentInspectorUI.Instance != null)
        {
            StudentInspectorUI.Instance.OpenForStudent(this);
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData) { originalPosition = transform.position; }

    public virtual void OnDrag(PointerEventData eventData)
    {
        RectTransform miRect = GetComponent<RectTransform>();
        if (miRect != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(miRect, eventData.position, eventData.pressEventCamera, out Vector3 posicionCorrecta);
            transform.position = posicionCorrecta;
        }
        else
        {
            Camera cam = _mainCamera != null ? _mainCamera : Camera.main;
            Vector3 posicionMouse = cam != null ? cam.ScreenToWorldPoint(eventData.position) : Vector3.zero;
            posicionMouse.z = 0f; 
            transform.position = posicionMouse;
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        bool dragExitoso = false;
        float snapRadius = 3f; 
        List<Seat> todasLasSillas = Seat.AllSeats;        
        Seat sillaMasCercana = null;
        float distanciaMinima = float.MaxValue;

        foreach (Seat silla in todasLasSillas)
        {
            float distancia = Vector2.Distance(transform.position, silla.transform.position);
            if (distancia < distanciaMinima && distancia <= snapRadius)
            {
                distanciaMinima = distancia;
                sillaMasCercana = silla;
            }
        }

        if (sillaMasCercana != null)
        {
            if (sillaMasCercana.currentStudent != null && sillaMasCercana.currentStudent != this)
            {
                Seat miSillaVieja = this.currentSeat;
                Seat suSillaVieja = sillaMasCercana;
                Student elOtroAlumno = sillaMasCercana.currentStudent;

                if (miSillaVieja != null)
                {
                    suSillaVieja.AssignStudent(this);
                    miSillaVieja.AssignStudent(elOtroAlumno);
                    AudioManager.Instance.PostEvent("Student_Change_Seats");
                    dragExitoso = true;
                }
            }
            else if (sillaMasCercana.currentStudent == null)
            {
                Seat miSillaVieja = this.currentSeat;
                if (miSillaVieja != null) miSillaVieja.currentStudent = null; 
                sillaMasCercana.AssignStudent(this); 
                dragExitoso = true;
            }
        }

        if (dragExitoso == false)
        {
            if (currentSeat != null) transform.position = currentSeat.transform.position;
            else transform.position = originalPosition;
        }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (!eventData.dragging) OnStudentClicked();
    }

    // --- EVALUACIÓN DE MULTIPLICADORES TOTALES (CACHEADO O(1)) ---
    public float GetTotalLearningMultiplier()
    {
        return _cachedLearningMultiplier;
    }

    public float GetTotalStressMultiplier()
    {
        return _cachedStressMultiplier;
    }
}