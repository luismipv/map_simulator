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
        // ¡NOS SUSCRIBIMOS AL NUEVO EVENTO!
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

    // --- ¡LA MAGIA DE INSTANCIAR EL TEXTO! ---
    // Cuando las herramientas mandan texto, ahora solo se forman en la fila
    private void SpawnFloatingText(string message, Color color)
    {
        if (floatingTextPrefab == null || floatingTextCanvas == null) return;

        // 1. Formamos el texto en la fila
        textQueue.Enqueue(new FloatingTextData { message = message, color = color });

        // 2. Si el despachador está dormido, lo despertamos
        if (!isSpawningText)
        {
            StartCoroutine(ProcessTextQueueRoutine());
        }
    }

    // El despachador que suelta los textos uno por uno
    private IEnumerator ProcessTextQueueRoutine()
    {
        isSpawningText = true;

        while (textQueue.Count > 0)
        {
            // Sacamos el primer texto formado en la fila
            FloatingTextData data = textQueue.Dequeue();

            // Lo creamos
            GameObject newText = Instantiate(floatingTextPrefab, floatingTextCanvas);
            
            // Le damos una posición central con un micro-margen aleatorio para dar dinamismo
            float randomX = Random.Range(-0.2f, 0.2f);
            newText.transform.localPosition = new Vector3(randomX, 0, 0);

            // Lo configuramos
            FloatingText ftScript = newText.GetComponent<FloatingText>();
            if (ftScript != null) ftScript.Setup(data.message, data.color);

            // ¡TU IDEA! Esperamos 0.2 segundos antes de procesar el siguiente en la fila
            yield return new WaitForSeconds(0.2f); 
        }

        // La fila está vacía, el despachador se va a dormir
        isSpawningText = false;
    }

    private void RefreshMultipliers()
    {
        if (multipliersText == null) return;

        string finalText = "";

        // 1. DIBUJAR TODOS LOS BUFFS DE APRENDIZAJE APILADOS
        foreach (var buff in studentCore.activeLearningBuffs)
        {
            if (buff.Value > 1f) 
                finalText += $"<color=#00FF00>{buff.Key} x{buff.Value}</color>\n";
            else if (buff.Value < 1f) 
                finalText += $"<color=#FF8C00>{buff.Key} x{buff.Value}</color>\n";
        }

        // 2. DIBUJAR TODOS LOS BUFFS DE ESTRÉS APILADOS
        foreach (var buff in studentCore.activeStressBuffs)
        {
            // Tratamiento especial para el reloj: Solo lo mostramos si ya hay verdadero pánico
            if (buff.Key == "¡Falta Poco! ⏰")
            {
                // Solo se dibuja en la UI si el multiplicador cruzó el umbral crítico (ej: 1.5x)
                if (buff.Value >= 1.8f) 
                {
                    finalText += $"<color=#FF5555>{buff.Key} x{buff.Value:F1}</color>\n";
                }
            }
            // Para todos los demás buffs de estrés normales:
            else 
            {
                if (buff.Value > 1f) 
                    finalText += $"<color=#FF0000>{buff.Key} x{buff.Value}</color>\n";
                else if (buff.Value < 1f) 
                    finalText += $"<color=#00FF00>{buff.Key} x{buff.Value}</color>\n";
            }
        }

        // 3. ESTADOS TEMPORALES INDEPENDIENTES (Como Flow)
        if (studentCore.currentState == StudentState.Flow)
        {
            finalText += $"<color=#00FFFF>¡En Flow!x3</color>\n"; 
        }

        multipliersText.text = finalText;
    }

}