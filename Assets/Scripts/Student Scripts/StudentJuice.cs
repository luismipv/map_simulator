using UnityEngine;

public class StudentJuice : MonoBehaviour
{
    private Student studentCore;

    [Header("Visuales")]
    public SpriteRenderer spriteRenderer; 
    public Color restingColor = Color.green;
    public Color workingColor = Color.white;
    public Color flowColor = Color.cyan;
    public Color burnoutColor = Color.red;
    public Color droppedOutColor = Color.gray;
    public Color distractedColor = new Color(1f, 0.5f, 0f);
    public Color hoverColor = Color.yellow;

    [Header("Shake Effect")]
    public float burnoutWarningThreshold = 85f; 
    public float shakeIntensity = 2f;           
    
    private Vector3 originalPosition;           
    private bool isShaking = false;             
    private Color colorOriginalDeEstado; 
    private bool isHovered = false;

    private void Awake()
    {
        studentCore = GetComponent<Student>();
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

    private void Update()
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
            state != StudentState.Resting) // Añadí Resting aquí para que no tiemblen si descansan
        {
            isShaking = true;
            transform.localPosition = originalPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeIntensity);
        }
        else if (isShaking)
        {
            transform.localPosition = originalPosition;
            isShaking = false;
        }
    }
}