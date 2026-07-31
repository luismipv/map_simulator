using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StudentUI : MonoBehaviour
{
    protected Student studentCore;

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
    public GameObject multipliersContainer;
    public GameObject multiplierPrefab;

    public Color positiveColor = new Color(0.3098039f, 0.6980392f, 0.5254902f);
    public Color negativeColor = new Color(0.9490197f, 0.427451f, 0.4941177f);
    public TextMeshProUGUI multipliersText;

    [Header("Dialogo de Burbuja")]
    public GameObject bubbleText;
    public GameObject bubbleBackground;
    public GameObject student;
    public float bubbleXOffset = 2f;
    public float bubbleYOffset = 4.5f;

    [Tooltip("Ancho máximo del texto antes de saltar a la siguiente línea")]
    public float maxBubbleWidth = 350f; 

    [Header("Márgenes de Libreta (Código)")]
    [Tooltip("Espacio extra a los lados y arriba del texto")]
    public Vector2 paddingLibreta = new Vector2(40f, 30f); 
    [Tooltip("Pixeles extra en la parte de abajo para proteger la colita")]
    public float alturaColita = 35f;

    private Coroutine hideBubbleCoroutine;
    private Coroutine animacionActual;

    // DICCIONARIO OPTIMIZADO: Ahora usa strings para separar Stress y Learning
    private Dictionary<string, Multiplier> multipliers = new Dictionary<string, Multiplier>();
    [SerializeField] public Dictionary<ModifierID, Sprite> multiplierIcons = new Dictionary<ModifierID, Sprite>();

    protected struct FloatingTextData
    {
        public string message;
        public Color color;
    }

    protected Queue<FloatingTextData> textQueue = new Queue<FloatingTextData>();
    private bool isSpawningText = false;

    private void Awake()
    {
        studentCore = GetComponent<Student>();
        Canvas myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) myCanvas.worldCamera = Camera.main;
        if (bubbleBackground != null) bubbleBackground.SetActive(false); 
    }

    private void OnEnable()
    {
        studentCore.OnStatsUpdated += UpdateSliders;
        studentCore.OnStateChanged += UpdateStateMessages;
        studentCore.OnJokeFeedbackEvent += HandleJokeFeedback;
        studentCore.OnFloatingTextRequested += SpawnFloatingText;
        studentCore.OnBubbleTextRequested += ShowBubble;
        
        // Conectamos la UI de modificadores al nuevo evento optimizado
        studentCore.OnModifiersChanged += RefreshMultipliersUI; 
    }

    private void OnDisable()
    {
        studentCore.OnStatsUpdated -= UpdateSliders;
        studentCore.OnStateChanged -= UpdateStateMessages;
        studentCore.OnJokeFeedbackEvent -= HandleJokeFeedback;
        studentCore.OnFloatingTextRequested -= SpawnFloatingText;
        studentCore.OnBubbleTextRequested -= ShowBubble;

        // Desconectamos para evitar fugas de memoria
        studentCore.OnModifiersChanged -= RefreshMultipliersUI; 
    }

    private void Start()
    {
        nameText.text = studentCore.studentName;
        if (studentCore.personalityData != null)
        {
            personalityText.text = $"Personalidad: {studentCore.personalityData.personalityNameEs}";
        }
        
        // Forzamos la actualización inicial de iconos
        RefreshMultipliersUI();
    }

    private void UpdateSliders(float stress, float learning)
    {
        stressText.text = Mathf.RoundToInt(stress / studentCore.maxStress * 100).ToString() + "%";
        stressSlider.value = stress / studentCore.maxStress; 

        learningText.text = Mathf.RoundToInt(learning / studentCore.maxLearning * 100).ToString() + "%";
        learningSlider.value = learning / studentCore.maxLearning; 

        // ELIMINADO: RefreshMultipliersUI(); (Ya no ocurre cada frame)
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
        if (likedIt) ShowBubble("😂", Color.green);
        else ShowBubble("🙄", Color.red);
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
            case ModifierID.Tool_Nag: return "Regaño 💢";
            case ModifierID.GlobalTool_Exam: return "Examen Sorpresa 📝";
            default: return id.ToString(); 
        }
    }

    private void RefreshMultipliers()
{
    if (multipliersText == null) return;
    string finalText = "";

    // 1. TEXTO PARA APRENDIZAJE
    foreach (var buff in studentCore.activeLearningBuffs)
    {
        if (Mathf.Approximately(buff.Value, 1f)) continue; // Ignoramos si es 1 exacto

        string uiName = GetModifierUIName(buff.Key); 
        if (buff.Value > 1f) 
            finalText += $"<color=#00FF00>{uiName} x{buff.Value}</color>\n";
        else 
            finalText += $"<color=#FF0000>{uiName} x{buff.Value}</color>\n";
    }

    // 2. TEXTO PARA ESTRÉS
    foreach (var buff in studentCore.activeStressBuffs)
    {
        if (Mathf.Approximately(buff.Value, 1f)) continue; // Ignoramos si es 1 exacto

        string uiName = GetModifierUIName(buff.Key); 
        
        if (buff.Key == ModifierID.FaltaPoco)
        {
            if (buff.Value >= 1.8f) 
                finalText += $"<color=#FF0000>{uiName} x{buff.Value:F1}</color>\n";
        }
        else 
        {
            if (buff.Value > 1f) 
                finalText += $"<color=#FF0000>{uiName} x{buff.Value}</color>\n";
            else 
                finalText += $"<color=#00FF00>{uiName} x{buff.Value}</color>\n";
        }
    }

    if (studentCore.currentState == StudentState.Flow)
    {
        finalText += $"<color=#00FFFF>¡En Flow!x3</color>\n"; 
    }

    multipliersText.text = finalText;
    }

    private void RefreshMultipliersUI() 
{
    if (multipliersContainer == null) return;

    // 1. DIBUJAR TODOS LOS BUFFS DE APRENDIZAJE APILADOS
    foreach (var buff in studentCore.activeLearningBuffs)
    {
        string key = "L_" + buff.Key.ToString();
        if (!multipliers.ContainsKey(key))
        {
            GameObject multiplier = Instantiate(multiplierPrefab, multipliersContainer.transform, false);
            multipliers[key] = multiplier.GetComponent<Multiplier>();
        }
        
        // APRENDIZAJE: Mayor a 1 es Positivo (Verde), Menor a 1 es Negativo (Rojo)
        Color targetColor = buff.Value > 1f ? positiveColor : negativeColor;
        
        multipliers[key].SetData(buff.Key, buff.Value, MultiplierIcons.GetIcon(buff.Key), targetColor);
    }

    // 2. DIBUJAR TODOS LOS BUFFS DE ESTRÉS APILADOS
    foreach (var buff in studentCore.activeStressBuffs)
    {
        string key = "S_" + buff.Key.ToString();
        if (!multipliers.ContainsKey(key))
        {
            GameObject multiplier = Instantiate(multiplierPrefab, multipliersContainer.transform, false);
            multipliers[key] = multiplier.GetComponent<Multiplier>();
        }
        
        Color targetColor;
        
        if (buff.Key == ModifierID.FaltaPoco)
        {
            targetColor = negativeColor;
            
            // Si FaltaPoco es menor a 1.8, lo apagamos por completo para no saturar la UI
            if (buff.Value < 1.8f) 
            {
                multipliers[key].gameObject.SetActive(false);
                continue; 
            }
        }
        else 
        {
            // ESTRÉS: Mayor a 1 es Negativo (Rojo), Menor a 1 es Positivo (Verde)
            targetColor = buff.Value > 1f ? negativeColor : positiveColor;
        }

        multipliers[key].SetData(buff.Key, buff.Value, MultiplierIcons.GetIcon(buff.Key), targetColor);
        }
    }

    public void ShowBubble(string message, Color color)
    {
        if (bubbleText != null && bubbleBackground != null)
        {
            bubbleBackground.SetActive(true);
            bubbleBackground.transform.localPosition = new Vector3(bubbleXOffset, bubbleYOffset, 0f);
            
            if (animacionActual != null) StopCoroutine(animacionActual);
            animacionActual = StartCoroutine(PopUpAnimationCoroutine(bubbleBackground));
            
            TMP_Text textComponent = bubbleText.GetComponent<TMP_Text>();
            
            if (textComponent != null)
            {
                textComponent.text = message;
                textComponent.color = color; 
                
                Vector2 textSize = textComponent.GetPreferredValues(message, maxBubbleWidth, Mathf.Infinity);
                RectTransform bgRect = bubbleBackground.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(textSize.x + paddingLibreta.x, textSize.y + paddingLibreta.y + alturaColita);

                RectTransform textRect = bubbleText.GetComponent<RectTransform>();
                textRect.sizeDelta = textSize; 
                
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0f, alturaColita / 2f);
            }
            else
            {
                Debug.LogError("¡OJO! No se encontró el componente TMP_Text en bubbleText.");
            }

            if (hideBubbleCoroutine != null) StopCoroutine(hideBubbleCoroutine);
            hideBubbleCoroutine = StartCoroutine(HideBubbleRoutine(2f)); 
        }
    }

    private IEnumerator HideBubbleRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideBubble();
    }

    public void HideBubble()
    {
        if (bubbleBackground != null && bubbleBackground.activeInHierarchy)
        {
            if (animacionActual != null) StopCoroutine(animacionActual);
            animacionActual = StartCoroutine(PopDownAnimationCoroutine(bubbleBackground));
        }
    }

    IEnumerator PopUpAnimationCoroutine(GameObject popup)
    {
        float duration = 0.1f; 
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one; 

        while (elapsed < duration) 
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            popup.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        
        popup.transform.localScale = targetScale;
    }

    private IEnumerator PopDownAnimationCoroutine(GameObject popup)
    {
        float duration = 0.1f; 
        float elapsed = 0f;
        Vector3 startScale = popup.transform.localScale; 
        
        if (bubbleText != null)
        {
            bubbleText.GetComponent<TMPro.TextMeshPro>().text = "";
        }

        while (elapsed < duration) 
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            popup.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        
        popup.SetActive(false);
    }
}