using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StudentJuice : MonoBehaviour
{
    private Student3D studentCore;

    [Header("Visuales")]
    public Image statusImageParent;
    public Image statusBackgroundContainer;
    public Image statusIconImage;
    public Image learningBarImage;
    public Image stressBarImage;
    public Image warningImage;

    [Space(10)]

    public Color restingColor = Color.green;
    public Color workingColor = Color.white;
    public Color flowColor = Color.cyan;
    public Color burnoutColor = Color.red;
    public Color droppedOutColor = Color.gray;
    public Color distractedColor = new Color(1f, 0.5f, 0f);
    public Color finishedColor = Color.gold;
    public Color hoverColor = Color.yellow;

    [Header("Icons")]
    public Sprite restingIcon;
    public Sprite workingIcon;
    public Sprite flowIcon;
    public Sprite burnoutIcon;
    public Sprite droppedOutIcon;
    public Sprite distractedIcon;
    public Sprite finishedIcon;

    [Header("Shake Effect")]
    public float burnoutWarningThreshold = 85f; 
    public float shakeIntensity = 0.02f;           
    
    private Vector3 originalPosition;           
    private bool isShaking = false;             
    private Color colorOriginalDeEstado; 
    private bool isHovered = false;
    private Coroutine activeFlashCoroutine;


   private void Start()
    {
        warningImage.enabled = false;
    }
   private void Awake()
    {
        studentCore = GetComponent<Student3D>();
        if (statusImageParent == null) statusImageParent = GetComponentInChildren<Image>();
        
        // Le pedimos la posición AL SPRITE, no al padre.
        originalPosition = statusImageParent.transform.localPosition; 
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
        if (newState == StudentState.Graduated && statusImageParent != null)
        {
            statusImageParent.enabled = false;
            statusIconImage.color = finishedColor;
            return;
        }

        switch (newState)
        {
            case StudentState.Resting: colorOriginalDeEstado = restingColor; statusIconImage.sprite = restingIcon; warningImage.enabled = false; break;
            case StudentState.Working: colorOriginalDeEstado = workingColor; statusIconImage.sprite = workingIcon; break;
            case StudentState.Flow: colorOriginalDeEstado = flowColor; statusIconImage.sprite = flowIcon; statusIconImage.color = colorOriginalDeEstado; break;
            case StudentState.Burnout: colorOriginalDeEstado = burnoutColor; statusIconImage.sprite = burnoutIcon; break;
            case StudentState.DroppedOut: colorOriginalDeEstado = droppedOutColor; statusIconImage.sprite = droppedOutIcon; break;
            case StudentState.Distracted: colorOriginalDeEstado = distractedColor; statusIconImage.sprite = distractedIcon; break;
            case StudentState.Finished: colorOriginalDeEstado = finishedColor; statusIconImage.sprite = finishedIcon; break;
        }

        if (statusImageParent != null && (StudentState.Distracted == newState || StudentState.Burnout == newState)){ 
            statusImageParent.color = colorOriginalDeEstado;
        }else {
            statusImageParent.color = Color.white;
        }
        if (statusIconImage != null){
            statusIconImage.color = colorOriginalDeEstado;
        }
    }

    private void HandleHover(bool hovering)
    {
        isHovered = hovering;
        if (statusBackgroundContainer != null)
        {
            statusBackgroundContainer.color = hovering ? hoverColor : Color.white;
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
        if (!isShaking)
        {
            AudioManager.Instance.PostEvent("Student_About_To_BurnOut", this.gameObject);
            
            // Detenemos cualquier parpadeo anterior por seguridad
            if (activeFlashCoroutine != null) StopCoroutine(activeFlashCoroutine);
            
            // Guardamos la nueva corrutina en la variable
            activeFlashCoroutine = StartCoroutine(FlashCoroutine(warningImage)); 
            
            studentCore.GetStudentVFX().ActivateSmoke();
            TutorialManager.Instance.ReportTrigger(TutorialTrigger.StudentAboutToBurnout);
        }
        isShaking = true;
        statusImageParent.transform.localPosition = originalPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeIntensity);
    }
    else if (isShaking)
    {
        statusImageParent.transform.localPosition = originalPosition;
        isShaking = false;
        studentCore.GetStudentVFX().DeactivateSmoke();
        
        // ¡Aquí detenemos la corrutina forzosamente!
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = null;
        }
        
        warningImage.enabled = false; 
    }
}
    
    private IEnumerator FlashCoroutine(Image warningImg)
{
    // El ciclo se repetirá infinitamente mientras siga trabajando
    while (studentCore.currentState == StudentState.Working)
    {
        warningImg.enabled = true;
        yield return new WaitForSeconds(0.5f);
        
        // Si el estado cambia a mitad de la espera, salimos
        if (studentCore.currentState != StudentState.Working) break;

        warningImg.enabled = false;
        yield return new WaitForSeconds(0.1f);
    }

    // Nos aseguramos de apagar la imagen cuando el ciclo se rompa
    warningImg.enabled = false;
}
}