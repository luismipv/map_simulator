using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ¡LA MAGIA DEL SINGLETON! Esto permite que otros scripts lo llamen usando UIManager.Instance
    public static UIManager Instance { get; private set; }

    [Header("Métricas Globales")]
    public Slider averageStressSlider;
    public TextMeshProUGUI averageStressText;
    public Slider averageLearningSlider;
    public TextMeshProUGUI averageLearningText;

    [Header("Flujo del Juego")]
    public Slider timerSlider;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI dropoutsText;
    public GameObject busyIndicatorUI;

    [Header("Pantalla Final")]
    public GameObject endGamePanel;
    public GameObject gameplayContainer;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI statsText;

    [Header("Exámenes Parciales")]
    public GameObject partialExamWarningUI;
    public TextMeshProUGUI nextExamText;
    public CanvasGroup tensionVignette;

    private void Awake()
    {
        // Configuramos el Singleton al inicio
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    // ==========================================
    // --- MÉTODOS PARA ACTUALIZAR LA PANTALLA ---
    // ==========================================

    public void UpdateMetrics(float stress, float learning)
    {
        if (averageStressText != null) averageStressText.text = $"Estrés General: {Mathf.RoundToInt(stress)}/100";
        if (averageStressSlider != null) averageStressSlider.value = stress / 100f; 

        if (averageLearningText != null) averageLearningText.text = $"Aprendizaje General: {Mathf.RoundToInt(learning)}/100";
        if (averageLearningSlider != null) averageLearningSlider.value = learning / 100f; 
    }

    public void UpdateTimer(float currentTime, float maxTime)
    {
        if (timerText != null) timerText.text = $"Tiempo Restante: {Mathf.RoundToInt(currentTime)}s"; 
        if (timerSlider != null) timerSlider.value = currentTime / maxTime; 
    }

    public void UpdateDropouts(int currentDropouts, int maxDropouts)
    {
        if (dropoutsText != null) dropoutsText.text = $"Bajas: {currentDropouts} / {maxDropouts}";
    }

    public void SetTeacherBusy(bool isBusy)
    {
        if (busyIndicatorUI != null) busyIndicatorUI.SetActive(isBusy);
    }

    public void UpdateExamUI(float timer, float timeToFade, float maxAlpha)
    {
        if (nextExamText != null) nextExamText.text = $"Siguiente Parcial: {Mathf.RoundToInt(timer)}s";
            
        if (tensionVignette != null)
        {
            if (timer <= timeToFade)
            {
                float fadePercentage = 1f - (timer / timeToFade);
                tensionVignette.alpha = fadePercentage * maxAlpha; 
            }
            else
            {
                tensionVignette.alpha = 0f; 
            }
        }
    }

    public void ShowExamWarning(bool isExamActive)
    {
        if (partialExamWarningUI != null) partialExamWarningUI.SetActive(isExamActive);
    }

    public void ShowEndScreen(bool isFired, bool perfectSemester, int grads, int dropouts, int maxDropouts, int totalStudents)
    {
        if (gameplayContainer != null) gameplayContainer.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(true);

        if (isFired)
        {
            if (resultTitleText != null) resultTitleText.text = "<color=red>¡DESPEDIDO!</color>";
            if (statsText != null) statsText.text = $"El sindicato te reportó.\n\nGraduados: {grads}\nBajas: {dropouts} / {maxDropouts}";
        }
        else if (perfectSemester)
        {
            if (resultTitleText != null) resultTitleText.text = "<color=green>¡SEMESTRE PERFECTO!</color>";
            if (statsText != null) statsText.text = "¡Increíble! Todos tus alumnos aprobaron con honores.\nTus superiores están orgullosos.";
        }
        else
        {
            if (resultTitleText != null) resultTitleText.text = "<color=yellow>¡SEMESTRE CONCLUIDO!</color>";
            if (statsText != null) statsText.text = $"Lograste terminar el año escolar.\n\nGraduados: {grads} / {totalStudents}\nBajas: {dropouts}";
        }
    }
}