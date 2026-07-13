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

        ChangeState(currentState); 
        OnStatsUpdated?.Invoke(stressLevel, learningLevel); 
    }

    void Update()
    {
        HandleStateLogic();
        CheckAutomaticTransitions();
        
        if (currentRestCooldown > 0f) currentRestCooldown -= Time.deltaTime;
        OnStatsUpdated?.Invoke(stressLevel, learningLevel);
    }

    public virtual void ChangeState(StudentState newState)
{
    if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated || currentState == StudentState.Finished) return;  
    
    currentState = newState;
    
    // 1. MATAMOS EL SONIDO GLOBALMENTE AL CAMBIAR DE ESTADO
    // Si estaba sonando, se calla. Si no estaba sonando, Wwise ignora esta línea.
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

    // CONTROL DE ESTADOS AL CAMBIAR (Ya sin la basura visual del audio)
    if (currentState == StudentState.Graduated)
    {
        ShowBubble("¡Se logró!", Color.yellow);
        TriggerGraduation();
    }
    else if (currentState == StudentState.Finished)
    {
        ShowBubble("¡Listo! ¿Quién necesita ayuda?", Color.yellow);
        AudioManager.Instance.PostEvent("Student_Finished", this.gameObject); 
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentTutor);
        learningLevel = maxLearning; 
        RemoveLearningModifier(ModifierID.Panico);
    }
    else if (currentState == StudentState.Flow) 
    {
        currentFlowTimer = flowDuration;
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentFlow);
    }
    else if (currentState == StudentState.Burnout)
    {
        currentBurnoutTimer = burnoutTimeLimit;
        ModifyLearningInstant(-20f); 
        AudioManager.Instance.PostEvent("Student_BurnedOut", this.gameObject); 
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentBurnout);
    }
    else if (currentState == StudentState.Distracted) 
    {
        contagionTimer = contagionInterval;
        RemoveLearningModifier(ModifierID.Panico);
        TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentDistracted);
    }
    else if (currentState == StudentState.Resting) 
    {
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
            //logicManager.graduatedStudents++; 
            if (logicManager.allStudents.Contains(this)) logicManager.allStudents.Remove(this);
        }

        Destroy(gameObject);
    }

    protected virtual void HandleStateLogic()
    {
        if (isExamMode) return;

        // Inyección del multiplicador de fin de semestre directo al Enum seguro
        if (logicManager != null)
        {
            AddStressModifier(ModifierID.FaltaPoco, logicManager.currentSemesterMultiplier);
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
        
        // 1. Leemos si el nivel permite distracciones
        bool distraccionesPermitidas = true;
        if (logicManager != null && logicManager.currentLevel != null)
        {
            distraccionesPermitidas = logicManager.currentLevel.enableDistractions;
        }

        // 2. ¡EL ESTUDIANTE LE PREGUNTA A SU PERSONALIDAD QUÉ HACER!
        if (personalityData != null)
        {
            personalityData.EvaluateSpecialBehaviors(this, distraccionesPermitidas);
        }

        // 3. Reglas Generales que aplican para TODOS
        if (currentState == StudentState.Resting && stressLevel <= 5f && distraccionesPermitidas)
        {
            // Calculamos la probabilidad basándonos en la variable de la personalidad
            float chanceBase = (personalityData != null) ? personalityData.distractionProbability : 5f;
            float probabilidadConvertida = chanceBase / 100f; 

            if (UnityEngine.Random.value < probabilidadConvertida * Time.deltaTime)
            {
                ChangeState(StudentState.Distracted);
                if (AudioManager.Instance != null) AudioManager.Instance.PostEvent("Student_Distracted", this.gameObject); 
                ShowBubble("Ya me aburrí...", Color.orange);
            } 
        }



        if (currentState == StudentState.Working && learningLevel > 50f && stressLevel >= 60f && stressLevel < 75f) 
        {
            ChangeState(StudentState.Flow);
        }
    }

    // --- INTERFAZ PÚBLICA SEGURA PARA COMPONENTES Y HERRAMIENTAS EXTEALAS ---
    public void AddLearningModifier(ModifierID id, float multiplier)
    {
        activeLearningBuffs[id] = multiplier;
    }

    public void RemoveLearningModifier(ModifierID id)
    {
        if (activeLearningBuffs.ContainsKey(id)) activeLearningBuffs.Remove(id);
    }

    public void AddStressModifier(ModifierID id, float multiplier)
    {
        activeStressBuffs[id] = multiplier;
    }

    public void RemoveStressModifier(ModifierID id)
    {
        if (activeStressBuffs.ContainsKey(id)) activeStressBuffs.Remove(id);
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
    // 1. Modificar valores matemáticos
    stressLevel = Mathf.Clamp(stressLevel + stressAmount, 0f, maxStress);
    learningLevel = Mathf.Clamp(learningLevel + learningAmount, 0f, maxLearning);

    // 2. Construir un solo texto combinado
    string combinedText = "";

    // Lógica para el texto de aprendizaje
    if (learningAmount > 0) 
        combinedText += $"<color=#00FF00>+{learningAmount}🧠 </color>" + "\n";
    else if (learningAmount < 0) combinedText += $"<color=#FF0000>{learningAmount}🧠 </color>" + "\n";

    // Lógica para el texto de estrés
    if (stressAmount > 0) combinedText += $"<color=#FF0000>+{stressAmount}💢 </color>" + "\n";
    else if (stressAmount < 0) combinedText += $"<color=#00FF00>{stressAmount}💢 </color>" + "\n";

    // 3. Enviar un solo Floating Text
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
        
        // Ahora llama al ToolManager en lugar del LogicManager
        if (ToolManager.Instance != null) ToolManager.Instance.ApplyToolToStudent(this);
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
            Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(eventData.position);
            posicionMouse.z = 0f; 
            transform.position = posicionMouse;
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        bool dragExitoso = false;
        float snapRadius = 3f; 
        Seat[] todasLasSillas = FindObjectsByType<Seat>(FindObjectsSortMode.None);        
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
                    AudioManager.Instance.PostEvent("Student_Change_Seats"); //SONIDO
                    Debug.Log($"¡{studentName} y {elOtroAlumno.studentName} intercambiaron asientos!");
                    dragExitoso = true;
                }
            }
            else if (sillaMasCercana.currentStudent == null)
            {
                Seat miSillaVieja = this.currentSeat;
                if (miSillaVieja != null) miSillaVieja.currentStudent = null; 
                sillaMasCercana.AssignStudent(this); 
                //AudioManager.Instance.PostEvent("Student_Change_Seats", this.gameObject); //SONIDO
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

    // --- EVALUACIÓN DE MULTIPLICADORES TOTALES ---
    public float GetTotalLearningMultiplier()
    {
        float total = 1f;
        foreach (float val in activeLearningBuffs.Values) total *= val; 
        return total;
    }

    public float GetTotalStressMultiplier()
    {
        float total = 1f;
        foreach (float val in activeStressBuffs.Values) total *= val;
        return total;
    }
}