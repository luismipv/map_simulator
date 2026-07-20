using System.Collections;
using UnityEngine;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Screens")]
    public RectTransform startOverlayScreen;
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
    public float startOverlayTransitionDuration = 1f;

    void Awake(){
        ShowStartOverlay();
    }

    void Start(){
        HideMainMenu();
    }

    public void ShowStartOverlay(){
        if (startOverlayScreen != null){
            startOverlayScreen.gameObject.SetActive(true);
            StartCoroutine(TransitionStartOverlay(true));
        }

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

    private IEnumerator TransitionStartOverlay(bool isVisible = false, float delay = 1f){
        if (startOverlayScreen == null) yield break;
        // Scale from 1.1 to 1 over 0.2 seconds for the startOverlayScreen
        float scaleElapsed = 0f;
        float scaleDuration = 0.2f;
        Vector3 initialScale = Vector3.one * 1.1f;
        Vector3 finalScale = Vector3.one;

        startOverlayScreen.localScale = initialScale;

        Vector2 hidePos = new Vector2(startOverlayScreen.offsetMax.x, -1000f);
        Vector2 showPos = new Vector2(startOverlayScreen.offsetMax.x, 0f);
        Vector2 minOffset = new Vector2(startOverlayScreen.offsetMin.x, -40f);

        startOverlayScreen.offsetMax = showPos;     // offsetMax.y is 'top'
        startOverlayScreen.offsetMin = minOffset;        // offsetMin.y is 'bottom'

        yield return new WaitForSeconds(delay);

        while (scaleElapsed < scaleDuration)
        {
            scaleElapsed += Time.deltaTime;
            float scaleT = Mathf.Clamp01(scaleElapsed / scaleDuration);
            startOverlayScreen.localScale = Vector3.Lerp(initialScale, finalScale, scaleT);
            yield return null;
        }
        startOverlayScreen.localScale = finalScale;



        Vector2 startPos = isVisible ? showPos : hidePos;
        //startOverlayScreen.anchoredPosition = startPos;
        Vector2 targetPos = isVisible ? hidePos : showPos;

        float elapsed = 0f;
        float duration = startOverlayTransitionDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = transitionCurve.Evaluate(t);

            // Interpolate only the 'top' value, keep bottom at -40
            float newTop = Mathf.Lerp(startPos.y, targetPos.y, curveT);
            startOverlayScreen.offsetMax = new Vector2(startOverlayScreen.offsetMax.x, newTop);     // offsetMax.y is 'top'
            startOverlayScreen.offsetMin = minOffset;        // offsetMin.y is 'bottom'
            
            yield return null;
 
        }

        //startOverlayScreen.anchoredPosition = targetPos;
        startOverlayScreen.offsetMax = hidePos;     // offsetMax.y is 'top'
        startOverlayScreen.offsetMin = new Vector2(startOverlayScreen.offsetMin.x, -40);        // offsetMin.y is 'bottom'

        if (isVisible){
            startOverlayScreen.gameObject.SetActive(false);
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
