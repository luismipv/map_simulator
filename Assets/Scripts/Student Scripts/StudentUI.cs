using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Sistema Flotante")]
    public GameObject floatingTextPrefab; 
    public Transform floatingTextCanvas; 

    [Header("Multiplicadores (UI Persistente)")]
    public TextMeshProUGUI multipliersText;

    private struct FloatingTextData
    {
        public string message;
        public Color color;
    }

    private Queue<FloatingTextData> textQueue = new Queue<FloatingTextData>();
    private bool isSpawningText = false;

    private void Awake()
    {
        studentCore = GetComponent<Student>();
        Canvas myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) myCanvas.worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        studentCore.OnStatsUpdated += UpdateSliders;
        studentCore.OnStateChanged += UpdateStateMessages;
        studentCore.OnJokeFeedbackEvent += HandleJokeFeedback;
        studentCore.OnFloatingTextRequested += SpawnFloatingText;
    }

    private void OnDisable()
    {
        studentCore.OnStatsUpdated -= UpdateSliders;
        studentCore.OnStateChanged -= UpdateStateMessages;
        studentCore.OnJokeFeedbackEvent -= HandleJokeFeedback;
        studentCore.OnFloatingTextRequested -= SpawnFloatingText;
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

        RefreshMultipliers();
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

    private void HandleJokeFeedback(bool likedIt)
    {
        if (likedIt) SpawnFloatingText("😂", Color.white);
        else SpawnFloatingText("🙄", Color.white);
    }

    private void SpawnFloatingText(string message, Color color)
    {
        if (floatingTextPrefab == null || floatingTextCanvas == null) return;

        textQueue.Enqueue(new FloatingTextData { message = message, color = color });

        if (!isSpawningText)
        {
            StartCoroutine(ProcessTextQueueRoutine());
        }
    }

    private IEnumerator ProcessTextQueueRoutine()
    {
        isSpawningText = true;

        while (textQueue.Count > 0)
        {
            FloatingTextData data = textQueue.Dequeue();

            GameObject newText = Instantiate(floatingTextPrefab, floatingTextCanvas, false);
            newText.transform.localScale = Vector3.one;

            float randomX = Random.Range(-0.2f, 0.2f);
            newText.transform.localPosition = new Vector3(randomX, 0.5f, 0);

            FloatingText ftScript = newText.GetComponent<FloatingText>();
            if (ftScript != null) ftScript.Setup(data.message, data.color);

            yield return new WaitForSeconds(0.2f); 
        }

        isSpawningText = false;
    }

    // --- EL TRADUCTOR VISUAL ---
    // Convierte el Enum matemático en un string amigable para el jugador
    private string GetModifierUIName(ModifierID id)
    {
        switch (id)
        {
            case ModifierID.Personalidad: return "Personalidad 👤";
            case ModifierID.Entorno: return "Entorno 🧠";
            case ModifierID.Sinergia: return "Sinergia 🔗"; 
            case ModifierID.Panico: return "Pánico 😱";
            case ModifierID.Tutor: return "Alumno Tutor 🎓";
            case ModifierID.FaltaPoco: return "¡Falta Poco! ⏰";
            case ModifierID.Tool_Tutoring: return "Asesoría 📚";
            //case ModifierID.Herramienta_Cafe: return "Café ☕";
            case ModifierID.Tool_Nag: return "Regaño 💢";
            case ModifierID.GlobalTool_Exam: return "Examen Sorpresa 📝";
            default: return id.ToString(); // Salvavidas por si olvidas agregar uno aquí
        }
    }

    private void RefreshMultipliers()
    {
        if (multipliersText == null) return;

        string finalText = "";

        // 1. DIBUJAR TODOS LOS BUFFS DE APRENDIZAJE APILADOS
        foreach (var buff in studentCore.activeLearningBuffs)
        {
            string uiName = GetModifierUIName(buff.Key); // Pasamos por el traductor

            if (buff.Value > 1f) 
                finalText += $"<color=#00FF00>{uiName} x{buff.Value}</color>\n";
            else if (buff.Value < 1f) 
                finalText += $"<color=#FF8C00>{uiName} x{buff.Value}</color>\n";
        }

        // 2. DIBUJAR TODOS LOS BUFFS DE ESTRÉS APILADOS
        foreach (var buff in studentCore.activeStressBuffs)
        {
            string uiName = GetModifierUIName(buff.Key); // Pasamos por el traductor

            // Tratamiento especial usando el Enum directamente
            if (buff.Key == ModifierID.FaltaPoco)
            {
                if (buff.Value >= 1.8f) 
                {
                    finalText += $"<color=#FF5555>{uiName} x{buff.Value:F1}</color>\n";
                }
            }
            else 
            {
                if (buff.Value > 1f) 
                    finalText += $"<color=#FF0000>{uiName} x{buff.Value}</color>\n";
                else if (buff.Value < 1f) 
                    finalText += $"<color=#00FF00>{uiName} x{buff.Value}</color>\n";
            }
        }

        // 3. ESTADOS TEMPORALES INDEPENDIENTES
        if (studentCore.currentState == StudentState.Flow)
        {
            finalText += $"<color=#00FFFF>¡En Flow!x3</color>\n"; 
        }

        multipliersText.text = finalText;
    }
}