using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // Para usar TextMeshProUGUI

public class GlobalToolsButtonUI: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Asigna la herramienta aquí")]
    public GlobalTool assignedTool; // ¡Aquí arrastras tu Scriptable Object!

    [Header("Referencias Visuales")] // ¡NUEVO!
    public TextMeshProUGUI buttonText;

    public Color colorNormal = Color.white;
    public Color colorActivo = Color.yellow;

    private Image buttonImage;
    private Button buttonComponent;
    private Logic gameLogic;
    
    void Start()
    {
        gameLogic = Object.FindAnyObjectByType<Logic>();
        buttonImage = GetComponent<Image>();
        buttonComponent = GetComponent<Button>();

        // Autoconfiguración estilo Mario Maker:
        if (assignedTool != null)
        {
            if (buttonImage != null && assignedTool.globalToolIcon != null)
            {
                buttonImage.sprite = assignedTool.globalToolIcon;
            }

            if (buttonText != null && assignedTool.globalToolName != null)
            {
                buttonText.text = assignedTool.globalToolName;
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
        if (gameLogic != null && assignedTool != null)
        {
            // 1. Pintamos el botón de color activo y lo desactivamos temporalmente para evitar doble clic
            if (buttonImage != null) buttonImage.color = colorActivo;
            buttonComponent.interactable = false;

            // 2. Ejecutamos la herramienta y le pasamos nuestra función "OnEffectFinished" como aviso
            assignedTool.ApplyGlobalToolEffect(gameLogic, OnEffectFinished);
        }
    }

    private void OnEffectFinished()
    {
        // 3. Restauramos el color original y volvemos a permitir clics
        if (buttonImage != null) buttonImage.color = colorNormal;
        buttonComponent.interactable = true;
    }

    // ==================================================
    // --- DETECCIÓN DE MOUSE (HOVER / TOOLTIP) ---
    // ==================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (assignedTool != null)
        {
            Debug.Log($"Mouse encima de: {assignedTool.globalToolName} -> Desc: {assignedTool.globalToolDescription}");
            // AQUÍ: Más adelante llamaremos a tu panel de Tooltip UI para mostrar la descripción
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // AQUÍ: Más adelante ocultaremos el panel de Tooltip UI
        Debug.Log("El mouse salió del botón.");
    }

        // ==================================================
    // --- ACTUALIZACIÓN VISUAL DEL BOTÓN ---
    // ==================================================
    public void UpdateVisualState(GlobalTool activeTool, Color normalColor, Color selectedColor)
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
            gameObject.name =  assignedTool.globalToolName; 

            // 2. ¡Bono extra! Cambia el texto visual en la escena inmediatamente
            if (buttonText != null)
            {
                buttonText.text = assignedTool.globalToolName;
            }
        }
    }
    
}
