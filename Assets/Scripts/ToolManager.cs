using UnityEngine;

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

    public void SelectTool(TeacherTool newTool)
    {
        currentModularTool = newTool;
        ToolButtonUI[] allButtons = UnityEngine.Object.FindObjectsByType<ToolButtonUI>(FindObjectsSortMode.None);
        
        foreach (ToolButtonUI btn in allButtons)
        {
            btn.UpdateVisualState(currentModularTool, colorNormal, colorSeleccionado);
        }
    }

    public void ApplyToolToStudent(Student student)
    {
        if (isTeacherBusy || currentModularTool == null || (Time.time < lastToolUsageTime + toolCooldown)) 
            return;

        AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject); 
        AudioManager.Instance.PostEvent("UI_Select", this.gameObject); 
        
        // Ejecutamos la herramienta usando el LogicManager como contexto
        currentModularTool.ApplyToolEffect(student, Logic.Instance); 
        
        lastToolUsageTime = Time.time;
    }

    public void SetTeacherBusy(bool busy)
    {
        isTeacherBusy = busy;
    }
}