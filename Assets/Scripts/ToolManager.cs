using UnityEngine;
using System.Collections.Generic;

public class ToolManager : MonoBehaviour
{
    public static ToolManager Instance { get; private set; }

    [Header("Herramientas del Maestro")]
    public TeacherTool currentModularTool;
    public Color colorNormal = Color.white;       
    public Color colorSeleccionado = Color.green;
    public bool isTeacherBusy = false;
    public float toolCooldown = 0.2f; 
    private float lastToolUsageTime = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- ESCUCHANDO EL INICIO DEL NIVEL ---
    void OnEnable()
    {
        Logic.OnGameStarted += SetupAvailableTools;
    }

    void OnDisable()
    {
        Logic.OnGameStarted -= SetupAvailableTools;
    }

    // --- FILTRANDO EL MAZO DE CARTAS ---
    private void SetupAvailableTools()
    {
        if (Logic.Instance == null || Logic.Instance.currentLevel == null) return;
        
        LevelData currentLevel = Logic.Instance.currentLevel;
        if (currentLevel.allowedTools == null) return;

        // ¡AQUÍ ESTÁ LA MAGIA! Agregamos FindObjectsInactive.Include para encontrar los apagados
        ToolButtonUI[] allButtons = UnityEngine.Object.FindObjectsByType<ToolButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ToolButtonUI btn in allButtons)
        {
            if (btn.assignedTool != null && currentLevel.allowedTools.Contains(btn.assignedTool))
            {
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        if (currentLevel.allowedTools.Count > 0)
        {
            SelectTool(currentLevel.allowedTools[0]);
        }
    }

    // --- FUNCIONES CLÁSICAS DEL MANAGER ---
    public void SelectTool(TeacherTool newTool)
    {
        currentModularTool = newTool;
        
        // También lo ponemos aquí por seguridad
        ToolButtonUI[] allButtons = UnityEngine.Object.FindObjectsByType<ToolButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (ToolButtonUI btn in allButtons)
        {
            //btn.UpdateVisualState(currentModularTool, colorNormal, colorSeleccionado);
        }
    }

    public void ApplyToolToStudent(Student student)
    {
        if (isTeacherBusy || currentModularTool == null || (Time.time < lastToolUsageTime + toolCooldown)) 
            return;

        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject); 
        AudioManager.Instance.PostEvent("UI_Select", this.gameObject); 
        
        currentModularTool.ApplyToolEffect(student, Logic.Instance); 
        
        lastToolUsageTime = Time.time;
    }

    public void SetTeacherBusy(bool busy)
    {
        isTeacherBusy = busy;
    }
}