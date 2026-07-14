using System.Collections;
using UnityEngine;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Screens")]
    public RectTransform mainMenuScreen;
    public RectTransform optionsScreen;
    public RectTransform exitScreen;
    public RectTransform creditsScreen;

    private bool isMainMenuVisible = false;
    private bool isOptionsVisible = false;
    private bool isExitVisible = false;
    private bool isCreditsVisible = false;

    [Header("Transition Settings")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float transitionDuration = 0.3f;

    void Start(){
        HideMainMenu();
    }

    public void HideMainMenu(){
        StartCoroutine(TransitionScreen(mainMenuScreen, false));
        isMainMenuVisible = false;
        if (optionsScreen != null){
            isOptionsVisible = false;
            StartCoroutine(TransitionScreen(optionsScreen, false));
        }
        if (exitScreen != null){
            isExitVisible = false;
            StartCoroutine(TransitionScreen(exitScreen, false));
        }
        if (creditsScreen != null){
            isCreditsVisible = false;
            StartCoroutine(TransitionScreen(creditsScreen, false));
        }
    }

    public void ShowMainMenu(){
        StartCoroutine(TransitionScreen(mainMenuScreen, true));
        isMainMenuVisible = true;
        if (optionsScreen != null){
            StartCoroutine(TransitionScreen(optionsScreen, true));
            isOptionsVisible = false;
        }
        if (exitScreen != null){
            StartCoroutine(TransitionScreen(exitScreen, true));
            isExitVisible = false;
        }   
        if (creditsScreen != null){
            StartCoroutine(TransitionScreen(creditsScreen, true));
            isCreditsVisible = false;
        }
    }

    public void ToggleMainMenu(){
        if (isMainMenuVisible){
            HideMainMenu();
        }else{
            ShowMainMenu();
        }
    }

    public void ShowOptions(){
        if (optionsScreen != null){
            isOptionsVisible = true;
            StartCoroutine(TransitionScreen(optionsScreen, isOptionsVisible));
            if (isMainMenuVisible){
                isMainMenuVisible = false;
                StartCoroutine(TransitionScreen(mainMenuScreen, isMainMenuVisible));
            }
            if (isExitVisible){
                isExitVisible = false;
                StartCoroutine(TransitionScreen(exitScreen, isExitVisible));
            }
            if (isCreditsVisible){
                isCreditsVisible = false;
                StartCoroutine(TransitionScreen(creditsScreen, isCreditsVisible));
            }
        }
    }

    public void ToggleOptions() {
        if (isOptionsVisible){
            HideMainMenu();
        }else{
            ShowOptions();
        }
    }

    private IEnumerator TransitionScreen(RectTransform screen, bool isVisible)
    {
        if (screen == null) yield break;

        Vector2 hidePos = new Vector2(screen.anchoredPosition.x, -640);
        Vector2 showPos = new Vector2(screen.anchoredPosition.x, -40);

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
