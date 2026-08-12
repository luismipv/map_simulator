using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudentInspectorUI : MonoBehaviour
{
    public static StudentInspectorUI Instance { get; private set; }

    [Header("Panel Principal")]
    public RectTransform inspectorPanelRect;
    public Button closeButton;

    [Header("Cabecera del Alumno")]
    public TextMeshProUGUI studentNameText;
    public TextMeshProUGUI personalityText;
    public Image personalityIcon;
    public TextMeshProUGUI stateBadgeText;
    public Image stateBadgeBackground;

    [Header("Métricas Rápidas")]
    public Slider stressSlider;
    public TextMeshProUGUI stressText;
    public Slider learningSlider;
    public TextMeshProUGUI learningText;

    [Header("Lista de Efectos (ScrollView)")]
    public Transform effectRowsContainer;
    public GameObject effectRowPrefab;
    public TextMeshProUGUI emptyEffectsText;

    [Header("Ajustes de Transición (Hoja Lateral)")]
    public float hiddenPositionX = 400f;
    public float visiblePositionX = 0f;
    public float transitionDuration = 0.3f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Student currentStudent;
    private bool isVisible = false;
    private Coroutine transitionCoroutine;

    private List<StudentEffectRowUI> spawnedRows = new List<StudentEffectRowUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void Start()
    {
        if (inspectorPanelRect != null)
        {
            inspectorPanelRect.anchoredPosition = new Vector2(hiddenPositionX, inspectorPanelRect.anchoredPosition.y);
        }
        if (emptyEffectsText != null) emptyEffectsText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromCurrentStudent();
    }

    public void OpenForStudent(Student student)
    {
        if (student == null) return;

        // Si es el mismo alumno y ya está visible, no reiniciamos animación
        if (currentStudent == student && isVisible)
        {
            RefreshUI();
            return;
        }

        UnsubscribeFromCurrentStudent();

        currentStudent = student;
        SubscribeToCurrentStudent();

        if (Logic.Instance != null) Logic.Instance.selectedStudent = student;

        RefreshUI();

        if (!isVisible)
        {
            isVisible = true;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionRoutine(true));
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PostEvent("UI_Select", gameObject);
        }
    }

    public void ClosePanel()
    {
        if (!isVisible) return;

        isVisible = false;
        UnsubscribeFromCurrentStudent();

        if (Logic.Instance != null && Logic.Instance.selectedStudent == currentStudent)
        {
            Logic.Instance.selectedStudent = null;
        }

        currentStudent = null;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(false));

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PostEvent("UI_Button_Press", gameObject);
        }
    }

    private void SubscribeToCurrentStudent()
    {
        if (currentStudent == null) return;
        currentStudent.OnStatsUpdated += HandleStatsUpdated;
        currentStudent.OnStateChanged += HandleStateChanged;
        currentStudent.OnModifiersChanged += HandleModifiersChanged;
    }

    private void UnsubscribeFromCurrentStudent()
    {
        if (currentStudent == null) return;
        currentStudent.OnStatsUpdated -= HandleStatsUpdated;
        currentStudent.OnStateChanged -= HandleStateChanged;
        currentStudent.OnModifiersChanged -= HandleModifiersChanged;
    }

    private void HandleStatsUpdated(float stress, float learning) { RefreshUI(); }
    private void HandleStateChanged(StudentState newState) { RefreshUI(); }
    private void HandleModifiersChanged() { RefreshUI(); }

    public void RefreshUI()
    {
        if (currentStudent == null) return;

        // 1. Datos Básicos
        if (studentNameText != null) studentNameText.text = currentStudent.studentName;
        
        if (personalityText != null)
        {
            string pName = currentStudent.personalityData != null ? currentStudent.personalityData.personalityNameEs : "Normal";
            personalityText.text = $"Personalidad: <b>{pName}</b>";
        }

        // 2. Estado Actual Badge
        if (stateBadgeText != null)
        {
            stateBadgeText.text = GetStateDisplayName(currentStudent.currentState);
        }

        // 3. Métricas
        if (stressSlider != null) stressSlider.value = currentStudent.stressLevel / currentStudent.maxStress;
        if (stressText != null) stressText.text = $"{Mathf.RoundToInt(currentStudent.stressLevel / currentStudent.maxStress * 100)}%";

        if (learningSlider != null) learningSlider.value = currentStudent.learningLevel / currentStudent.maxLearning;
        if (learningText != null) learningText.text = $"{Mathf.RoundToInt(currentStudent.learningLevel / currentStudent.maxLearning * 100)}%";

        // 4. Lista de Efectos con Frases
        PopulateEffectRows();
    }

    private string GetStateDisplayName(StudentState state)
    {
        switch (state)
        {
            case StudentState.Working: return "Trabajando 🟢";
            case StudentState.Flow: return "¡En Flow! ⚡";
            case StudentState.Distracted: return "Distraído 🟡";
            case StudentState.Burnout: return "Burnout 🔥";
            case StudentState.Resting: return "Descansando 💤";
            case StudentState.Finished: return "Terminado 🎓";
            case StudentState.DroppedOut: return "Baja ❌";
            case StudentState.Graduated: return "Graduado 🏆";
            default: return state.ToString();
        }
    }

    private void PopulateEffectRows()
    {
        if (effectRowsContainer == null || effectRowPrefab == null) return;

        // Limpiar filas viejas
        foreach (Transform child in effectRowsContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedRows.Clear();

        int effectCount = 0;

        // --- A. EFECTOS DE ESTADO ESPECIAL (Flow, Burnout, Distracted) ---
        if (currentStudent.currentState == StudentState.Flow)
        {
            CreateRow("Estado de Flow", "x3.0", $"¡Imparable! {currentStudent.studentName} alcanzó la máxima concentración.", true, ModifierID.Sinergia);
            effectCount++;
        }
        else if (currentStudent.currentState == StudentState.Distracted)
        {
            CreateRow("Distracción", "Baja Concentración", $"{currentStudent.studentName} perdió el hilo de la clase y no está aprendiendo.", false, ModifierID.Tool_Nag);
            effectCount++;
        }
        else if (currentStudent.currentState == StudentState.Burnout)
        {
            CreateRow("Burnout", "Colapso", $"{currentStudent.studentName} colapsó por exceso de estrés.", false, ModifierID.Panico);
            effectCount++;
        }

        // --- B. BUFFS Y DEBUFFS DE APRENDIZAJE ---
        foreach (var buff in currentStudent.activeLearningBuffs)
        {
            if (Mathf.Approximately(buff.Value, 1f)) continue;

            bool isPositive = buff.Value > 1f;
            string badge = isPositive ? $"+{Mathf.RoundToInt((buff.Value - 1f) * 100)}%" : $"-{Mathf.RoundToInt((1f - buff.Value) * 100)}%";
            string title = GetModifierTitle(buff.Key);
            string phrase = GetFlavorPhrase(buff.Key, buff.Value, isPositive, true);

            Sprite icon = GetModifierIcon(buff.Key);
            CreateRow(title, badge, phrase, isPositive, buff.Key, icon);
            effectCount++;
        }

        // --- C. BUFFS Y DEBUFFS DE ESTRÉS ---
        foreach (var buff in currentStudent.activeStressBuffs)
        {
            if (Mathf.Approximately(buff.Value, 1f)) continue;
            if (buff.Key == ModifierID.FaltaPoco && buff.Value < 1.8f) continue;

            bool isPositive = buff.Value < 1f; // En estrés, < 1 es positivo (reduce estrés)
            string badge = isPositive ? $"-{Mathf.RoundToInt((1f - buff.Value) * 100)}% Estrés" : $"+{Mathf.RoundToInt((buff.Value - 1f) * 100)}% Estrés";
            string title = GetModifierTitle(buff.Key);
            string phrase = GetFlavorPhrase(buff.Key, buff.Value, isPositive, false);

            Sprite icon = GetModifierIcon(buff.Key);
            CreateRow(title, badge, phrase, isPositive, buff.Key, icon);
            effectCount++;
        }

        if (emptyEffectsText != null)
        {
            emptyEffectsText.gameObject.SetActive(effectCount == 0);
        }
    }

    private void CreateRow(string title, string badge, string phrase, bool isPositive, ModifierID modifierID, Sprite overrideIcon = null)
    {
        GameObject newRowObj = Instantiate(effectRowPrefab, effectRowsContainer);
        StudentEffectRowUI rowUI = newRowObj.GetComponent<StudentEffectRowUI>();
        if (rowUI != null)
        {
            Sprite icon = overrideIcon != null ? overrideIcon : GetModifierIcon(modifierID);
            rowUI.SetupEffect(title, badge, phrase, isPositive, icon);
            spawnedRows.Add(rowUI);
        }
    }

    private Sprite GetModifierIcon(ModifierID id)
    {
        try
        {
            return MultiplierIcons.GetIcon(id);
        }
        catch
        {
            return null;
        }
    }

    private string GetModifierTitle(ModifierID id)
    {
        switch (id)
        {
            case ModifierID.Entorno: return "Ubicación en el Salón";
            case ModifierID.Sinergia: return "Sinergia con Compañeros";
            case ModifierID.Tutor: return "Compañero Tutor";
            case ModifierID.Tool_Tutoring: return "Asesoría del Profesor";
            case ModifierID.Tool_Nag: return "Llamada de Atención";
            case ModifierID.Tool_Relax: return "Tiempo de Descanso";
            case ModifierID.Tool_Homework: return "Refuerzo de Tarea";
            case ModifierID.Panico: return "Bloqueo por Estrés";
            case ModifierID.FaltaPoco: return "Cierre del Parcial";
            case ModifierID.GlobalTool_Exam: return "Examen Sorpresa";
            default: return id.ToString();
        }
    }

    private string GetFlavorPhrase(ModifierID id, float value, bool isPositive, bool isLearning)
    {
        string name = currentStudent != null ? currentStudent.studentName : "El alumno";
        StudentPersonality pType = currentStudent?.personalityData != null ? currentStudent.personalityData.personalityType : StudentPersonality.Normal;

        switch (id)
        {
            case ModifierID.Entorno:
                if (SpatialManager.Instance != null)
                {
                    if (currentStudent.transform.position.z >= SpatialManager.Instance.zFilaFrente)
                    {
                        return isPositive
                            ? $"A {name} le encanta estar hasta adelante. Su concentración aumenta."
                            : $"A {name} le estresa estar hasta adelante frente al profesor.";
                    }
                    else if (currentStudent.transform.position.z <= SpatialManager.Instance.zFilaAtras)
                    {
                        return isPositive
                            ? $"A {name} le fascina la fila de atrás. Se siente relajado en su zona."
                            : $"{name} siente que pierde visibilidad del pizarrón estando hasta atrás.";
                    }
                }
                return isPositive
                    ? $"{name} se siente cómodo con su posición actual en el salón."
                    : $"{name} se encuentra incómodo con su asiento actual.";

            case ModifierID.Sinergia:
                return isPositive
                    ? $"¡Buena química! {name} tiene una excelente relación y sinergia con sus vecinos."
                    : $"Mala influencia: {name} choca o se distrae con la cercanía de sus vecinos.";

            case ModifierID.Tutor:
                return $"¡Aprendizaje acelerado! {name} está aprendiendo gracias a un compañero tutor cercano.";

            case ModifierID.Tool_Tutoring:
                return $"{name} está recibiendo asesoría directa y atención personalizada del maestro.";

            case ModifierID.Tool_Nag:
                return $"{name} siente la presión tras la llamada de atención recibida.";

            case ModifierID.Tool_Relax:
                return $"{name} está aprovechando un momento de descanso para reducir su nivel de estrés.";

            case ModifierID.Tool_Homework:
                return $"{name} reforzó sus conocimientos tras entregar su tarea a tiempo.";

            case ModifierID.Panico:
                return $"{name} está sufriendo un bloqueo mental repentino debido al alto estrés.";

            case ModifierID.FaltaPoco:
                return $"¡El tiempo vuela! {name} siente la tensión por el cierre inminente del parcial.";

            case ModifierID.GlobalTool_Exam:
                return $"{name} está respondiendo bajo presión el examen sorpresa.";

            default:
                return isPositive
                    ? $"{name} cuenta con un efecto favorable en este momento."
                    : $"{name} experimenta una penalización momentánea.";
        }
    }

    private IEnumerator TransitionRoutine(bool show)
    {
        if (inspectorPanelRect == null) yield break;

        Vector2 startPos = inspectorPanelRect.anchoredPosition;
        Vector2 targetPos = new Vector2(show ? visiblePositionX : hiddenPositionX, startPos.y);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float curveT = transitionCurve != null ? transitionCurve.Evaluate(t) : t;
            inspectorPanelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);
            yield return null;
        }

        inspectorPanelRect.anchoredPosition = targetPos;
    }
}
