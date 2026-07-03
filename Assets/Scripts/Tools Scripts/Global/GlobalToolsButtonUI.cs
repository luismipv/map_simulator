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
    public float alturaExtra = 10f; // Altura extra para el efecto de hover
    private Vector3 posicionBase;
    private bool estaElevado = false;

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
        // Solo lo subimos si no está ya arriba
        if (estaElevado == false) 
        {
            // Leemos dónde lo acomodó el Layout Group exactamente en este momento
            posicionBase = transform.localPosition;
            
            // Lo empujamos hacia arriba
            transform.localPosition = new Vector3(posicionBase.x, posicionBase.y + alturaExtra, posicionBase.z);
            estaElevado = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (estaElevado == true)
        {
            // Lo regresamos a su posición base
            transform.localPosition = posicionBase;
            estaElevado = false;
        }
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
