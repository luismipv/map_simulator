using UnityEngine;

public class StudentJuice : MonoBehaviour
{
    private Student3D studentCore;

    [Header("Visuales")]
    public SpriteRenderer spriteRenderer; 
    public Color restingColor = Color.green;
    public Color workingColor = Color.white;
    public Color flowColor = Color.cyan;
    public Color burnoutColor = Color.red;
    public Color droppedOutColor = Color.gray;
    public Color distractedColor = new Color(1f, 0.5f, 0f);
    public Color finishedColor = Color.gold;
    public Color hoverColor = Color.yellow;

    [Header("Shake Effect")]
    public float burnoutWarningThreshold = 85f; 
    public float shakeIntensity = 0.02f;           
    
    private Vector3 originalPosition;           
    private bool isShaking = false;             
    private Color colorOriginalDeEstado; 
    private bool isHovered = false;

    private void Awake()
    {
        studentCore = GetComponent<Student3D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        studentCore.OnStateChanged += UpdateColor;
        studentCore.OnHoverChanged += HandleHover;
    }

    private void OnDisable()
    {
        studentCore.OnStateChanged -= UpdateColor;
        studentCore.OnHoverChanged -= HandleHover;
    }

    private void FixedUpdate()
    {
        // Le preguntamos al cerebro su estrés actual para saber si temblamos
        HandleShakeEffect(studentCore.stressLevel, studentCore.currentState);
    }

    private void UpdateColor(StudentState newState)
    {
        if (newState == StudentState.Graduated && spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        switch (newState)
        {
            case StudentState.Resting: colorOriginalDeEstado = restingColor; break;
            case StudentState.Working: colorOriginalDeEstado = workingColor; break;
            case StudentState.Flow: colorOriginalDeEstado = flowColor; break;
            case StudentState.Burnout: colorOriginalDeEstado = burnoutColor; break;
            case StudentState.DroppedOut: colorOriginalDeEstado = droppedOutColor; break;
            case StudentState.Distracted: colorOriginalDeEstado = distractedColor; break;
            case StudentState.Finished: colorOriginalDeEstado = finishedColor; break;
        }

        if (!isHovered && spriteRenderer != null) spriteRenderer.color = colorOriginalDeEstado;
    }

    private void HandleHover(bool hovering)
    {
        isHovered = hovering;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hovering ? hoverColor : colorOriginalDeEstado;
        }
    }

        private void HandleShakeEffect(float currentStress, StudentState state)
    {
        if (currentStress >= burnoutWarningThreshold && 
            state != StudentState.Burnout && 
            state != StudentState.DroppedOut && 
            state != StudentState.Graduated && 
            state != StudentState.Resting)
        {
            isShaking = true;
            
            // ¡MAGIA PURA! Movemos SOLAMENTE el dibujo (el hijo).
            // Como su papá es el Asiento, su "centro" siempre es Vector3.zero
            spriteRenderer.transform.localPosition = Vector3.zero + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeIntensity);
            studentCore.GetStudentVFX().ActivateSmoke();
        }
        else if (isShaking)
        {
            // Regresamos el dibujo a su centro exacto (0,0,0)
            spriteRenderer.transform.localPosition = Vector3.zero;
            isShaking = false;
            studentCore.GetStudentVFX().DeactivateAllParticles();
        }
    }
}