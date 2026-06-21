using UnityEngine;
using UnityEngine.EventSystems;
using System; // Necesario para los Eventos (Action)
using System.Collections.Generic; // Necesario para Listas y Diccionarios

// Los Enums se quedan igual, fuera de la clase
public enum StudentState { Working, Flow, Burnout, Resting, DroppedOut, Distracted, Graduated }
public enum StudentPersonality { Normal, Nerd, Slacker, Anxious }

public class Student : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler,IPointerClickHandler
{
    [Header("Datos del Estudiante")]
    public string studentName = "Juan Perez"; 
    public StudentState currentState = StudentState.Working; 
    public StudentPersonalitySO personalityData;

    [Header("Sistema de Asientos")]
    public Seat currentSeat;
    private Vector3 originalPosition; // Para regresar si lo sueltas en la nada
    private int originalSiblingIndex; // Para el orden visual (opcional)

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

    [HideInInspector] public float stressMultiplier = 1f;
    [HideInInspector] public float learningMultiplier = 1f;    

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
    private Logic logicManager; 

    // ==========================================
    // --- LOS MEGÁFONOS (EVENTOS) ---
    // ==========================================
    // Otros scripts se suscribirán a estos eventos para reaccionar
    public event Action<float, float> OnStatsUpdated; // Manda: stressLevel, learningLevel
    public event Action<StudentState> OnStateChanged; // Manda: el nuevo estado
    public event Action<string, string, float, GameObject> OnFeedbackRequested; // Para los mensajes
    public event Action<bool> OnHoverChanged; // Para que el Juice sepa si el mouse está encima

    public event Action<bool> OnJokeFeedbackEvent; // Nuevo evento para feedback de chistes

    void Start()
    {
        logicManager = FindAnyObjectByType<Logic>();

        if (personalityData != null)
        {
            workingLearningRate *= personalityData.learningRateMod;
            workingStressRate *= personalityData.stressRateMod;
            restingRecoveryRate *= personalityData.recoveryRateMod;
        }

        float stressVariance = UnityEngine.Random.Range(0.85f, 1.15f);
        float learningVariance = UnityEngine.Random.Range(0.85f, 1.15f);
        workingStressRate *= stressVariance;
        workingLearningRate *= learningVariance;

        ChangeState(currentState); 
        OnStatsUpdated?.Invoke(stressLevel, learningLevel); // Disparamos la actualización inicial
    }

    void Update()
    {
        HandleStateLogic();
        CheckAutomaticTransitions();
        
        if (currentRestCooldown > 0f) currentRestCooldown -= Time.deltaTime;
        
        // Avisamos a la UI constantemente de los números actuales
        OnStatsUpdated?.Invoke(stressLevel, learningLevel);
    }

    public void ChangeState(StudentState newState)
    {
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) return;  
        currentState = newState;
        
        if (currentState == StudentState.Graduated)
        {
            Debug.Log(studentName + " ¡Aprobó y se fue a casa!");
        }
        else if (currentState == StudentState.Flow) currentFlowTimer = flowDuration;
        else if (currentState == StudentState.Burnout)
        {
            currentBurnoutTimer = burnoutTimeLimit;
            ModifyLearningInstant(-20f); 
        }
        else if (currentState == StudentState.Distracted) contagionTimer = contagionInterval;
        else if (currentState == StudentState.Resting) currentRestTimer = mandatoryRestDuration;

        // ¡Gritamos por el megáfono que el estado cambió!
        OnStateChanged?.Invoke(currentState); 
    }

    private void HandleStateLogic()
    {
        switch (currentState)
        {
            case StudentState.Resting:
                stressLevel -= restingRecoveryRate * Time.deltaTime;
                learningLevel -= (restingRecoveryRate * 0.05f) * Time.deltaTime;
                currentRestTimer -= Time.deltaTime;
                if (currentRestTimer <= 0f) ChangeState(StudentState.Working);
                break;
            
             case StudentState.Working:
                float panicMult = (logicManager != null) ? logicManager.currentSemesterMultiplier : 1f;
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
                if (currentBurnoutTimer <= 0f) ChangeState(StudentState.DroppedOut);
                break;
                
            case StudentState.DroppedOut:
                learningLevel = 0f;
                stressLevel = maxStress;
                break;

            case StudentState.Distracted:
                stressLevel -= (restingRecoveryRate * 0.1f) * Time.deltaTime; 
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
        }

        stressLevel = Mathf.Clamp(stressLevel, 0f, maxStress);
        learningLevel = Mathf.Clamp(learningLevel, 0f, maxLearning);
    }

    private void CheckAutomaticTransitions()
    {
        if (learningLevel >= maxLearning && currentState != StudentState.Graduated) { ChangeState(StudentState.Graduated); return; }
        if (stressLevel >= maxStress && currentState != StudentState.Burnout) { ChangeState(StudentState.Burnout); return; }
        
        if (currentState == StudentState.Working && personalityData != null && personalityData.personalityType == StudentPersonality.Slacker && stressLevel < 40f)
        {
            if (UnityEngine.Random.value < 0.15f * Time.deltaTime) ChangeState(StudentState.Distracted);
        }
        else if (currentState == StudentState.Resting && stressLevel <= 5f)
        {
            if (UnityEngine.Random.value < 0.35f * Time.deltaTime) ChangeState(StudentState.Distracted);
        }

        if (currentState == StudentState.Working && learningLevel > 50f && stressLevel >= 60f) ChangeState(StudentState.Flow);
        else if (currentState == StudentState.Flow && stressLevel > 75f) ChangeState(StudentState.Working);
    }

    public void ModifyStressInstant(float amount) { stressLevel = Mathf.Clamp(stressLevel + amount, 0f, maxStress); }
    public void ModifyLearningInstant(float amount) { learningLevel = Mathf.Clamp(learningLevel + amount, 0f, maxLearning); }

    // Métodos públicos que llaman los botones/herramientas
    public void RequestJokeFeedback(bool likedIt)
    {
        OnJokeFeedbackEvent?.Invoke(likedIt);
    }

    public void RequestDistractionFeedback(bool success, string partnerName)
    {
        if (success) OnFeedbackRequested?.Invoke($"<color=yellow>¡Chismeando con {partnerName}!</color>", "🗣️ 📱", 2.5f, null);
        else OnFeedbackRequested?.Invoke("<color=#808080>¡Shh! Déjame trabajar.</color>", "🛑", 2.5f, null);
    }

    public void RequestExamFeedback(bool passed)
    {
        if (passed) OnFeedbackRequested?.Invoke("<color=#FFD700>¡Aprobado! 😎</color>", "¡Uff, qué alivio!", 3f, null);
        else OnFeedbackRequested?.Invoke("<color=#DC143C>¡Reprobado! 😱</color>", "¡A estudiar más!", 3f, null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (logicManager != null && logicManager.selectedStudent != null && logicManager.selectedStudent != this) return;
        OnHoverChanged?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData) { OnHoverChanged?.Invoke(false); }
    public void OnStudentClicked()
    {
        if (currentState == StudentState.DroppedOut || currentState == StudentState.Graduated) return;
        if (logicManager != null) logicManager.ApplyToolToStudent(this);
    }

        // 1. Cuando haces el primer clic y empiezas a mover el mouse
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position; 
    }

    // 2. Mientras mueves el mouse por la pantalla
    public void OnDrag(PointerEventData eventData)
    {
        RectTransform miRect = GetComponent<RectTransform>();
        
        // CASO A: Si tus alumnos son de UI
        if (miRect != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                miRect, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector3 posicionCorrecta);
            
            transform.position = posicionCorrecta;
        }
        // CASO B: Si tus alumnos son objetos 2D en el mundo
        else
        {
            // ¡La magia está aquí! Usamos eventData.position en lugar de Input.mousePosition
            Vector3 posicionMouse = Camera.main.ScreenToWorldPoint(eventData.position);
            posicionMouse.z = 0f; // Evita que se sumerjan en el eje Z
            transform.position = posicionMouse;
        }
    }

    // 3. Cuando sueltas el clic
       public void OnEndDrag(PointerEventData eventData)
    {
        bool dragExitoso = false;

        // VISIÓN DE RAYOS X: Disparamos un rayo que guarda TODO lo que toca
        List<RaycastResult> resultados = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, resultados);

        // Revisamos todo lo que atravesó el rayo
        foreach (RaycastResult resultado in resultados)
        {
            GameObject objetoTocado = resultado.gameObject;

            // CASO 1: ¿Atravesamos a otro Alumno?
            Student targetStudent = objetoTocado.GetComponentInParent<Student>();
            if (targetStudent != null && targetStudent != this)
            {
                Seat myOldSeat = this.currentSeat;
                Seat hisOldSeat = targetStudent.currentSeat;

                if (myOldSeat != null && hisOldSeat != null)
                {
                    hisOldSeat.AssignStudent(this);
                    myOldSeat.AssignStudent(targetStudent);
                    dragExitoso = true;
                    break; // ¡Éxito! Dejamos de buscar
                }
            }

            // CASO 2: ¿Atravesamos un Asiento vacío?
            Seat targetSeat = objetoTocado.GetComponentInParent<Seat>();
            if (targetSeat != null && targetSeat.currentStudent == null)
            {
                Seat myOldSeat = this.currentSeat;
                if (myOldSeat != null) myOldSeat.currentStudent = null;

                targetSeat.AssignStudent(this); 
                dragExitoso = true;
                break; // ¡Éxito! Dejamos de buscar
            }
        }

        // CASO 3: Si lo soltamos en la nada
        if (dragExitoso == false)
        {
            if (currentSeat != null) transform.position = currentSeat.transform.position;
            else transform.position = originalPosition;
        }
    }

     public void OnPointerClick(PointerEventData eventData)
    {
        // Si el evento NO fue un arrastre, entonces fue un clic genuino
        if (!eventData.dragging)
        {
            OnStudentClicked();
        }
    }
}