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

    private Image buttonImage;
    private Button buttonComponent;
    
    // ¡ELIMINAMOS la referencia a 'Logic' porque ese script ya no existe!

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonComponent = GetComponent<Button>();

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
    public void UpdateVisualState(TeacherTool activeTool, Color normalColor, Color selectedColor)
    {
        if (buttonImage != null)
        {
            // Si la herramienta de este botón es la que está activa, se pinta verde. Si no, blanco.
            buttonImage.color = (assignedTool == activeTool) ? selectedColor : normalColor;
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