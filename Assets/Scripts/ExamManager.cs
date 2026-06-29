using UnityEngine;
using System.Collections;

public class ExamManager : MonoBehaviour
{
    // ¡Nuestro Singleton!
    public static ExamManager Instance { get; private set; }

    [Header("Exámenes Parciales Automáticos")]
    public float partialExamInterval = 100f; 
    private float nextExamTimer;             
    public float timeToStartFading = 30f; 
    public float maxTensionAlpha = 0.5f; 

    private Logic gameLogic;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start()
    {
        gameLogic = Object.FindAnyObjectByType<Logic>();
        nextExamTimer = partialExamInterval;
        
        // El ExamManager ahora se encarga de apagar el letrero al inicio
        //if (UIManager.Instance != null) UIManager.Instance.ShowExamWarning(false);
    }

    private void Update()
    {
        // Solo corre si el juego no está pausado y si encontró el Logic
        if (Time.timeScale == 0f || gameLogic == null) return; 

        //HandlePartialExams();
    }

  /*  private void HandlePartialExams()
    {
        nextExamTimer -= Time.deltaTime;

        // El UIManager hace las matemáticas para oscurecer la viñeta
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateExamUI(nextExamTimer, timeToStartFading, maxTensionAlpha);
        }

        if (nextExamTimer <= 0f)
        {
            StartCoroutine(PartialExamRoutine());
            nextExamTimer = partialExamInterval; 
        }
    }

    private IEnumerator PartialExamRoutine()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowExamWarning(true);
        Time.timeScale = 0f; // Pausa el juego

        yield return new WaitForSecondsRealtime(2f);

        // Evalúa a todos los alumnos
        foreach (Student s in gameLogic.allStudents)
        {
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            if (s.learningLevel >= (s.maxLearning / 2f))
            {
                s.ModifyStressInstant(-35f); 
            }
            else
            {
                s.ModifyStressInstant(40f);
            }
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowExamWarning(false);
        Time.timeScale = 1f; // Reanuda el juego
    }
*/
    // Método público para que Logic pueda reiniciar el reloj cuando hay cambio de salón
    public void ResetExamTimer()
    {
        nextExamTimer = partialExamInterval;
        if (UIManager.Instance != null) UIManager.Instance.UpdateExamUI(nextExamTimer, timeToStartFading, maxTensionAlpha);
    }
}