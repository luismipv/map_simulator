using UnityEngine;
using System.Collections.Generic;

public class GlobalToolManager : MonoBehaviour
{
    // --- ESCUCHANDO EL INICIO DEL NIVEL ---
    void OnEnable()
    {
        Logic.OnGameStarted += SetupAvailableGlobalTools;
    }

    void OnDisable()
    {
        Logic.OnGameStarted -= SetupAvailableGlobalTools;
    }

    // --- EL FILTRO (RAYOS X INCLUIDOS) ---
    private void SetupAvailableGlobalTools()
    {
        if (Logic.Instance == null || Logic.Instance.currentLevel == null) return;
        
        LevelData currentLevel = Logic.Instance.currentLevel;
        
        // Prevención de errores si a alguien se le olvidó configurar la lista
        if (currentLevel.allowedGlobalTools == null) return;

        // Buscamos todos los botones globales en la escena (¡Incluso los apagados!)
        GlobalToolsButtonUI[] allGlobalButtons = UnityEngine.Object.FindObjectsByType<GlobalToolsButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GlobalToolsButtonUI btn in allGlobalButtons)
        {
            // Verificamos si la herramienta de este botón está en la lista permitida del nivel
            if (btn.assignedTool != null && currentLevel.allowedGlobalTools.Contains(btn.assignedTool))
            {
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }
}