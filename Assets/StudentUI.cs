using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StudentUI : MonoBehaviour
{
    private Student studentCore;

    [Header("UI General")]
    public Slider stressSlider; 
    public Slider learningSlider; 
    public TextMeshProUGUI stressText; 
    public TextMeshProUGUI learningText; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI personalityText;

    [Header("UI Mensajes Fijos")]
    public GameObject burnedOutMesssage;
    public GameObject flowMessage;

    [Header("Íconos de Feedback (Chistes)")]
    public GameObject happyFaceIcon;
    public GameObject angryFaceIcon;

    private void Awake()
    {
        // Buscamos el cerebro en este mismo objeto
        studentCore = GetComponent<Student>();
        Canvas myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) myCanvas.worldCamera = Camera.main;
    }

    // NOS SUSCRIBIMOS A LOS EVENTOS
    private void OnEnable()
    {
        studentCore.OnStatsUpdated += UpdateSliders;
        studentCore.OnStateChanged += UpdateStateMessages;
        studentCore.OnFeedbackRequested += TriggerFeedback;
        studentCore.OnJokeFeedbackEvent += HandleJokeFeedback;
    }

    // NOS DESUSCRIBIMOS (Súper importante para evitar errores si el alumno se destruye)
    private void OnDisable()
    {
        studentCore.OnStatsUpdated -= UpdateSliders;
        studentCore.OnStateChanged -= UpdateStateMessages;
        studentCore.OnFeedbackRequested -= TriggerFeedback;
        studentCore.OnJokeFeedbackEvent -= HandleJokeFeedback;
    }

    private void Start()
    {
        nameText.text = studentCore.studentName;
        if (studentCore.personalityData != null)
        {
            personalityText.text = $"Personalidad: {studentCore.personalityData.personalityNameEs}";
        }
       
    }

    private void UpdateSliders(float stress, float learning)
    {
        stressText.text = Mathf.RoundToInt(stress / studentCore.maxStress * 100).ToString() + "%";
        stressSlider.value = stress / studentCore.maxStress; 

        learningText.text = Mathf.RoundToInt(learning / studentCore.maxLearning * 100).ToString() + "%";
        learningSlider.value = learning / studentCore.maxLearning; 
    }

    private void UpdateStateMessages(StudentState newState)
    {
        if (newState == StudentState.Graduated)
        {
            
            stressSlider.gameObject.SetActive(false);
            learningSlider.gameObject.SetActive(false);
            stressText.gameObject.SetActive(false);
            learningText.gameObject.SetActive(false);
            personalityText.gameObject.SetActive(false);
            nameText.text = $"{studentCore.studentName}\n¡Graduado!";
        }

        if (burnedOutMesssage != null) burnedOutMesssage.SetActive(newState == StudentState.Burnout);
        if (flowMessage != null) flowMessage.SetActive(newState == StudentState.Flow);
    }

        private void TriggerFeedback(string message, string subtitle, float duration, GameObject icon)
    {
        StartCoroutine(UnifiedFeedbackRoutine(message, subtitle, duration, icon));
    }

     private void HandleJokeFeedback(bool likedIt)
    {
        if (likedIt) 
        {
            StartCoroutine(UnifiedFeedbackRoutine($"{studentCore.studentName} dice: ¡Jajaja!", "", 2f, happyFaceIcon));
        }
        else 
        {
            StartCoroutine(UnifiedFeedbackRoutine($"{studentCore.studentName} dice: ¡Malo!", "", 2f, angryFaceIcon));
        }
    }

    private IEnumerator UnifiedFeedbackRoutine(string message, string subtitle, float duration, GameObject iconToShow)
    {
        if (studentCore.currentState == StudentState.DroppedOut || studentCore.currentState == StudentState.Graduated) yield break;

        if (iconToShow != null) iconToShow.SetActive(true);
        nameText.text = message;
        personalityText.text = subtitle;

        yield return new WaitForSeconds(duration);

        if (iconToShow != null) iconToShow.SetActive(false);

        if (studentCore.currentState != StudentState.DroppedOut && studentCore.currentState != StudentState.Graduated)
        {
            nameText.text = studentCore.studentName;
            string pName = (studentCore.personalityData != null) ? studentCore.personalityData.personalityNameEs : "Desconocida";
            personalityText.text = $"Personalidad: {pName}";
        }
    }
}