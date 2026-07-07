using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ExamPenaltyMode { PanicAttack, MoneyFine, Snowball }

public class ExamManager : MonoBehaviour
{
    public static ExamManager Instance { get; private set; }

    // Enchufe para el Tutorial
    public static event Action OnExamPhaseStarted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // El LogicManager llamará a esta función pasándole la lista de alumnos y el nivel
    public void EvaluateClass(List<Student> allStudents, LevelData currentLevel, int currentPartial, float currentQuota, int currentMoney, Action<int> onExamFinished)
    {
        OnExamPhaseStarted?.Invoke();
        
        // Bloqueamos las manos del maestro
        if (ToolManager.Instance != null) ToolManager.Instance.SetTeacherBusy(true);

        int passedStudents = 0;
        int failedStudents = 0;
        int moneyEarnedThisRound = 0;
        List<StudentEvalData> studentsToAnimate = new List<StudentEvalData>();

        for (int i = allStudents.Count - 1; i >= 0; i--)
        {
            Student s = allStudents[i];
            if (s == null || s.currentState == StudentState.DroppedOut) continue;

            float finalLearning = s.learningLevel;
            float currentStress = s.stressLevel;
            bool isGraduatedThisRound = false;
            
            ExamPenaltyMode modeApplied = ExamPenaltyMode.PanicAttack; 

            // ATAQUE DE PÁNICO Y MULTAS
            if (currentStress >= 80f)
            {
                finalLearning -= 20f; 
                
                if (currentLevel.enableMoneyFines) 
                {
                    moneyEarnedThisRound -= 25; 
                    modeApplied = ExamPenaltyMode.MoneyFine;
                }
            }

            // EVALUACIÓN FINAL
            if (finalLearning >= currentQuota || s.currentState == StudentState.Finished)
            {
                passedStudents++;
                moneyEarnedThisRound += currentLevel.moneyPerPass;

                if (currentPartial >= currentLevel.totalPartials)
                {
                    isGraduatedThisRound = true;
                    s.ChangeState(StudentState.Graduated);
                }
            }
            else failedStudents++;

            studentsToAnimate.Add(new StudentEvalData {
                studentName = s.studentName,
                rawLearning = s.learningLevel, 
                rawStress = s.stressLevel,
                penaltyMode = modeApplied,
                isGraduated = isGraduatedThisRound
            });

            s.learningLevel = 0f; 
            s.stressLevel = 0f; 
            s.isExamMode = true; 
        }

        int finalMoney = currentMoney + moneyEarnedThisRound;
        if (finalMoney < 0) finalMoney = 0; 

        // Arrancamos el Show de Animación
        StartCoroutine(ShowResultsWithDelay(passedStudents, failedStudents, moneyEarnedThisRound, finalMoney, ExamPenaltyMode.PanicAttack, studentsToAnimate, currentLevel, currentPartial, currentQuota, onExamFinished));
    }

    private IEnumerator ShowResultsWithDelay(int passed, int failed, int moneyEarned, int totalMoney, ExamPenaltyMode mode, List<StudentEvalData> evalData, LevelData level, int currentPartial, float quota, Action<int> onExamFinished)
    {
        float delayTime = (currentPartial >= level.totalPartials) ? 2.5f : 1.0f;
        yield return new WaitForSeconds(delayTime);
        
        UIManager.Instance.ShowExamResults(passed, failed, moneyEarned, totalMoney, mode, "");
        UIManager.Instance.evaluationScreen.ShowAllResults(evalData, Mathf.RoundToInt(quota));
        
        // Le regresamos el dinero actualizado al LogicManager
        onExamFinished?.Invoke(totalMoney);
    }
}

// Reubicamos el struct aquí (o puedes dejarlo hasta abajo, es global)
[System.Serializable]
public struct StudentEvalData
{
    public string studentName;
    public float rawLearning;
    public float rawStress;
    public ExamPenaltyMode penaltyMode;
    public bool isGraduated;
}