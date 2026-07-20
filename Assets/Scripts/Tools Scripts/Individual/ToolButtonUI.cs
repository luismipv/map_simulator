using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // Para usar TextMeshProUGUI

public class ToolButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Asigna la herramienta aquí")]
    public TeacherTool assignedTool; // ¡Aquí arrastras tu Scriptable Object!

    [Header("Referencias Visuales")] 
    public TextMeshProUGUI buttonText;

    [Header("Configuración de Color")]
    [Tooltip("El color normal de este botón específico")]
    public Color baseColor = Color.white; 
    
    [Tooltip("Qué tanto aumentará la saturación al seleccionarse (0 a 1)")]
    [Range(0f, 1f)] public float saturationBoost = 0.5f;
    
    [Tooltip("Qué tanto se oscurecerá al seleccionarse (0 a 1)")]
    [Range(0f, 1f)] public float darknessBoost = 0.7f;

    private Image buttonImage;
    [SerializeField]private Image marker;
    private Button buttonComponent;
    private Color selectedColor; // Lo calculamos en secreto
    
    // ¡ELIMINAMOS la referencia a 'Logic' porque ese script ya no existe!

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonComponent = GetComponent<Button>();

        // --- MAGIA MATEMÁTICA PARA EL COLOR SELECCIONADO ---
        Color.RGBToHSV(baseColor, out float hue, out float saturation, out float value);
        
        // Aumentamos saturación (máximo 1) y bajamos brillo (mínimo 0)
        float newSaturation = Mathf.Clamp01(saturation + saturationBoost);
        float newValue = Mathf.Clamp01(value - darknessBoost);
        
        // Convertimos de vuelta a RGB y lo guardamos
        selectedColor = Color.HSVToRGB(hue, newSaturation, newValue);

        // Autoconfiguración estilo Mario Maker:
        if (assignedTool != null)
        {
            if (buttonImage != null && assignedTool.toolIcon != null)
            {
                buttonImage.sprite = assignedTool.toolIcon;
            }

            if (buttonText != null && assignedTool.toolName != null)
            {
                buttonText.text = assignedTool.toolName;
            }

            // Escuchamos el clic nativo del botón y le avisamos a la lógica global
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(SelectThisTool);
            }
        }

        // Pintamos el botón con su color normal al iniciar
        if (buttonImage != null)
        {
            buttonImage.color = baseColor;
        }
    }

    private void SelectThisTool()
    {
        // Ahora nos comunicamos directamente con el nuevo jefe: ToolManager
        if (ToolManager.Instance != null && assignedTool != null)
        {
            ToolManager.Instance.SelectTool(assignedTool);
            
            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.PostEvent("UI_Button_Press", this.gameObject); //SONIDO
            }
        }
    }

    // ==================================================
    // --- DETECCIÓN DE MOUSE (HOVER / TOOLTIP) ---
    // ==================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (assignedTool != null)
        {
            // AQUÍ: Más adelante llamaremos a tu panel de Tooltip UI para mostrar la descripción
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // AQUÍ: Más adelante ocultaremos el panel de Tooltip UI
    }

    // ==================================================
    // --- ACTUALIZACIÓN VISUAL DEL BOTÓN ---
    // ==================================================
    
    // ¡NUEVO! Ya no recibe colores del Manager, el botón sabe exactamente de qué color pintarse
    public void UpdateVisualState(TeacherTool activeTool)
    {
        if (buttonImage != null)
        {
            // Si la herramienta de este botón es la que está activa, usa la versión saturada y oscura
            buttonImage.color = (assignedTool == activeTool) ? selectedColor : baseColor;
            buttonImage.transform.localScale = (assignedTool != activeTool) ? Vector3.one : Vector3.one * 0.975f;
        }
        if (marker != null)
        {
            marker.gameObject.SetActive(assignedTool == activeTool);
        }
    }

    // ==================================================
    // --- MAGIA DEL EDITOR (SE EJECUTA SIN DARLE PLAY) ---
    // ==================================================
    private void OnValidate()
    {
        // Solo hacemos esto si ya le asignaste una herramienta en el Inspector
        if (assignedTool != null)
        {
            // 1. Cambia el nombre del GameObject en tu Hierarchy
            gameObject.name =  assignedTool.toolName; 

            // 2. ¡Bono extra! Cambia el texto visual en la escena inmediatamente
            if (buttonText != null)
            {
                buttonText.text = assignedTool.toolName;
            }
        }
    }
}