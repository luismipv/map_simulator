using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class StudentInspectorSetupTool : Editor
{
    [MenuItem("MAP Simulator/Auto-Setup Student Inspector UI")]
    public static void AutoSetupUI()
    {
        // 1. Buscar o crear Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 2. Buscar o crear UIManager
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();

        // 3. Crear el Panel Deslizable Principal
        GameObject panelObj = GameObject.Find("StudentInspectorPanel");
        if (panelObj == null)
        {
            panelObj = new GameObject("StudentInspectorPanel", typeof(RectTransform), typeof(Image), typeof(StudentInspectorUI));
            panelObj.transform.SetParent(canvas.transform, false);
        }

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(360f, 0f);
        panelRect.anchoredPosition = new Vector2(400f, 0f); // Oculto por defecto

        Image bgImage = panelObj.GetComponent<Image>();
        bgImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f); // Fondo oscuro elegante

        StudentInspectorUI inspectorUI = panelObj.GetComponent<StudentInspectorUI>();
        inspectorUI.inspectorPanelRect = panelRect;

        // 4. Crear Botón Cerrar "X"
        GameObject closeBtnObj = FindOrCreateChild(panelObj, "CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.sizeDelta = new Vector2(35f, 35f);
        closeBtnRect.anchoredPosition = new Vector2(-10f, -10f);

        closeBtnObj.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 1f);
        inspectorUI.closeButton = closeBtnObj.GetComponent<Button>();

        GameObject closeTextObj = FindOrCreateChild(closeBtnObj, "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI closeTMP = closeTextObj.GetComponent<TextMeshProUGUI>();
        closeTMP.text = "X";
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.fontSize = 20;
        closeTMP.color = Color.white;
        closeTextObj.GetComponent<RectTransform>().sizeDelta = closeBtnRect.sizeDelta;

        // 5. Cabecera (Nombre y Personalidad)
        GameObject headerObj = FindOrCreateChild(panelObj, "HeaderArea", typeof(RectTransform));
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(-40f, 90f);
        headerRect.anchoredPosition = new Vector2(0f, -15f);

        GameObject nameObj = FindOrCreateChild(headerObj, "StudentNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        nameTMP.text = "Nombre del Alumno";
        nameTMP.fontSize = 22;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 30f);
        nameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
        inspectorUI.studentNameText = nameTMP;

        GameObject personalityObj = FindOrCreateChild(headerObj, "PersonalityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI personalityTMP = personalityObj.GetComponent<TextMeshProUGUI>();
        personalityTMP.text = "Personalidad: Normal";
        personalityTMP.fontSize = 15;
        personalityTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        personalityObj.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 25f);
        personalityObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -30f);
        inspectorUI.personalityText = personalityTMP;

        GameObject stateBadgeObj = FindOrCreateChild(headerObj, "StateBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI stateBadgeTMP = stateBadgeObj.GetComponent<TextMeshProUGUI>();
        stateBadgeTMP.text = "Trabajando 🟢";
        stateBadgeTMP.fontSize = 14;
        stateBadgeTMP.color = new Color(0.3f, 0.9f, 0.5f, 1f);
        stateBadgeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 25f);
        stateBadgeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -55f);
        inspectorUI.stateBadgeText = stateBadgeTMP;

        // 6. Sección de Métricas (Estrés y Aprendizaje)
        GameObject metricsObj = FindOrCreateChild(panelObj, "MetricsArea", typeof(RectTransform));
        RectTransform metricsRect = metricsObj.GetComponent<RectTransform>();
        metricsRect.anchorMin = new Vector2(0f, 1f);
        metricsRect.anchorMax = new Vector2(1f, 1f);
        metricsRect.pivot = new Vector2(0.5f, 1f);
        metricsRect.sizeDelta = new Vector2(-40f, 100f);
        metricsRect.anchoredPosition = new Vector2(0f, -115f);

        // Estrés
        GameObject stressLabelObj = FindOrCreateChild(metricsObj, "StressText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI stressTMP = stressLabelObj.GetComponent<TextMeshProUGUI>();
        stressTMP.text = "Estrés: 0%";
        stressTMP.fontSize = 14;
        stressTMP.color = new Color(0.95f, 0.45f, 0.45f, 1f);
        stressLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 20f);
        stressLabelObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
        inspectorUI.stressText = stressTMP;

        GameObject stressSliderObj = FindOrCreateChild(metricsObj, "StressSlider", typeof(RectTransform), typeof(Slider));
        Slider stressSld = stressSliderObj.GetComponent<Slider>();
        stressSliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 15f);
        stressSliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -22f);
        inspectorUI.stressSlider = stressSld;

        // Aprendizaje
        GameObject learnLabelObj = FindOrCreateChild(metricsObj, "LearningText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI learnTMP = learnLabelObj.GetComponent<TextMeshProUGUI>();
        learnTMP.text = "Aprendizaje: 0%";
        learnTMP.fontSize = 14;
        learnTMP.color = new Color(0.35f, 0.85f, 0.55f, 1f);
        learnLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 20f);
        learnLabelObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -47f);
        inspectorUI.learningText = learnTMP;

        GameObject learnSliderObj = FindOrCreateChild(metricsObj, "LearningSlider", typeof(RectTransform), typeof(Slider));
        Slider learnSld = learnSliderObj.GetComponent<Slider>();
        learnSliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 15f);
        learnSliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -69f);
        inspectorUI.learningSlider = learnSld;

        // 7. ScrollView para Lista de Efectos
        GameObject scrollObj = FindOrCreateChild(panelObj, "EffectsScrollView", typeof(RectTransform), typeof(ScrollRect));
        RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.offsetMin = new Vector2(15f, 15f);
        scrollRectTransform.offsetMax = new Vector2(-15f, -230f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();

        GameObject viewportObj = FindOrCreateChild(scrollObj, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.05f);

        GameObject contentObj = FindOrCreateChild(viewportObj, "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObj.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        inspectorUI.effectRowsContainer = contentObj.transform;

        // 8. Crear Prefab de Fila de Efecto si no existe
        GameObject rowPrefabObj = CreateEffectRowPrefab();
        inspectorUI.effectRowPrefab = rowPrefabObj;

        // 9. Conectar a UIManager si existe en escena
        if (uiManager != null)
        {
            uiManager.studentInspectorPanel = inspectorUI;
            EditorUtility.SetDirty(uiManager);
        }

        EditorUtility.SetDirty(panelObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelObj.scene);

        Debug.Log("✅ ¡StudentInspectorUI creado y conectado automáticamente con éxito!");
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName, params System.Type[] components)
    {
        Transform childTr = parent.transform.Find(childName);
        GameObject childObj;
        if (childTr == null)
        {
            childObj = new GameObject(childName, components);
            childObj.transform.SetParent(parent.transform, false);
        }
        else
        {
            childObj = childTr.gameObject;
            foreach (var comp in components)
            {
                if (childObj.GetComponent(comp) == null) childObj.AddComponent(comp);
            }
        }
        return childObj;
    }

    private static GameObject CreateEffectRowPrefab()
    {
        string dirPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(dirPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string prefabPath = "Assets/Resources/StudentEffectRowPrefab.prefab";
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null) return existingPrefab;

        GameObject rowObj = new GameObject("StudentEffectRowPrefab", typeof(RectTransform), typeof(Image), typeof(StudentEffectRowUI), typeof(LayoutElement));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(310f, 75f);

        LayoutElement layoutElement = rowObj.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 75f;
        layoutElement.minHeight = 75f;

        Image bgImage = rowObj.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.22f, 0.28f, 0.9f);

        StudentEffectRowUI rowUI = rowObj.GetComponent<StudentEffectRowUI>();
        rowUI.cardBackground = bgImage;

        // Icono
        GameObject iconObj = FindOrCreateChild(rowObj, "IconImage", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(35f, 35f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        rowUI.iconImage = iconObj.GetComponent<Image>();

        // Título
        GameObject titleObj = FindOrCreateChild(rowObj, "TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-150f, 22f);
        titleRect.anchoredPosition = new Vector2(50f, -8f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "Título del Efecto";
        titleTMP.fontSize = 13;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        rowUI.titleText = titleTMP;

        // Badge
        GameObject badgeObj = FindOrCreateChild(rowObj, "BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = new Vector2(130f, 22f);
        badgeRect.anchoredPosition = new Vector2(-10f, -8f);

        TextMeshProUGUI badgeTMP = badgeObj.GetComponent<TextMeshProUGUI>();
        badgeTMP.text = "x1.2 (Aprendizaje)";
        badgeTMP.fontSize = 11;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Right;
        badgeTMP.color = new Color(0.3f, 0.85f, 0.5f, 1f);
        rowUI.badgeText = badgeTMP;

        // Frase
        GameObject phraseObj = FindOrCreateChild(rowObj, "FlavorPhraseText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform phraseRect = phraseObj.GetComponent<RectTransform>();
        phraseRect.anchorMin = new Vector2(0f, 0f);
        phraseRect.anchorMax = new Vector2(1f, 1f);
        phraseRect.pivot = new Vector2(0f, 0f);
        phraseRect.offsetMin = new Vector2(50f, 8f);
        phraseRect.offsetMax = new Vector2(-10f, -32f);

        TextMeshProUGUI phraseTMP = phraseObj.GetComponent<TextMeshProUGUI>();
        phraseTMP.text = "A Luismi le encanta estar hasta adelante. Su aprendizaje aumenta.";
        phraseTMP.fontSize = 11;
        phraseTMP.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        phraseTMP.enableWordWrapping = true;
        rowUI.flavorPhraseText = phraseTMP;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowObj, prefabPath);
        DestroyImmediate(rowObj);
        return prefab;
    }
}
