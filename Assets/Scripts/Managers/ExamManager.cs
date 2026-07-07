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


    // Método público para que Logic pueda reiniciar el reloj cuando hay cambio de salón
    public void ResetExamTimer()
    {
        nextExamTimer = partialExamInterval;
        if (UIManager.Instance != null) UIManager.Instance.UpdateExamUI(nextExamTimer, timeToStartFading, maxTensionAlpha);
    }
}