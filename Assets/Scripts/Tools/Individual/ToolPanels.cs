using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToolPanels : MonoBehaviour
{
    [Header("Screens")]
    public RectTransform actionToolsPanel;
    public RectTransform globalToolsPanel;

    private bool isActionToolsPanelVisible = false;
    private bool isGlobalToolsPanelVisible = false;

    [Header("Transition Settings")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float transitionDuration = 0.3f;

    void Start(){
        HideActionToolsPanel();
        CheckInactivePanels();
    }

    void CheckInactivePanels() {
        bool actionToolsPanelActive = actionToolsPanel && actionToolsPanel.transform.childCount > 0;
        if (!actionToolsPanelActive){
            isActionToolsPanelVisible = false;
            actionToolsPanel.GetComponent<Button>().interactable = false;
        }
        bool globalToolsPanelActive = globalToolsPanel && globalToolsPanel.transform.childCount > 0;
        if (!globalToolsPanelActive){
            isGlobalToolsPanelVisible = false;
            globalToolsPanel.GetComponent<Button>().interactable = false;
        }
    }

    public void HideActionToolsPanel(){
        StartCoroutine(TransitionScreen(actionToolsPanel, false));
        isActionToolsPanelVisible = false;
        if (globalToolsPanel != null){
            isGlobalToolsPanelVisible = false;
            StartCoroutine(TransitionScreen(globalToolsPanel, false));
        }
    }

    public void ShowActionToolsPanel(){
        StartCoroutine(TransitionScreen(actionToolsPanel, true));
        isActionToolsPanelVisible = true;
        if (globalToolsPanel != null){
            StartCoroutine(TransitionScreen(globalToolsPanel, true));
            isGlobalToolsPanelVisible = false;
        }
    }

    public void ToggleActionToolsPanel(){
        if (isActionToolsPanelVisible){
            HideActionToolsPanel();
        }else{
            ShowActionToolsPanel();
        }
    }

    public void ShowGlobalToolsPanel(){
        if (globalToolsPanel != null){
            isGlobalToolsPanelVisible = true;
            StartCoroutine(TransitionScreen(globalToolsPanel, isGlobalToolsPanelVisible));
            if (isActionToolsPanelVisible){
                isActionToolsPanelVisible = false;
                StartCoroutine(TransitionScreen(actionToolsPanel, isActionToolsPanelVisible));
            }
        }
    }

    public void ToggleGlobalToolsPanel() {
        if (isGlobalToolsPanelVisible){
            HideActionToolsPanel();
        }else{
            ShowGlobalToolsPanel();
        }
    }

    private IEnumerator TransitionScreen(RectTransform screen, bool isVisible)
    {
        if (screen == null) yield break;

        Vector2 hidePos = new Vector2(-370, screen.anchoredPosition.y);
        Vector2 showPos = new Vector2(0, screen.anchoredPosition.y);

        Vector2 startPos = screen.anchoredPosition;
        Vector2 targetPos = isVisible ? showPos : hidePos;

        float elapsed = 0f;
        float duration = transitionDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = transitionCurve.Evaluate(t);
            screen.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);
            yield return null;
        }

        screen.anchoredPosition = targetPos;
    }
}
