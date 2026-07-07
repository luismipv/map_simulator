using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class StudentResultRow : MonoBehaviour
{
    [Header("Textos de la Fila")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI learningText;
    public TextMeshProUGUI stressText;
    public TextMeshProUGUI penaltyText;
    public TextMeshProUGUI verdictText;

    [Header("Barras Visuales (Efecto Bloodborne)")]
    public Slider learningSlider; // La barra verde (frente)
    public Slider stressSlider;   // La barra roja fantasma (atrás)

    [Header("Ritmo")]
    public float countSpeed = 0.02f;

    public void Animate(StudentEvalData data, int passingQuota)
    {
        StartCoroutine(RunAnimationFlow(data, passingQuota));
    }

    private IEnumerator RunAnimationFlow(StudentEvalData data, int passingQuota)
    {
        // 1. LIMPIEZA
        nameText.text = data.studentName;
        learningText.text = "0";
        stressText.text = "";
        penaltyText.text = "";
        verdictText.text = "";
        
        if(learningSlider != null) learningSlider.value = 0f;
        if(stressSlider != null) stressSlider.value = 0f;

        yield return new WaitForSeconds(Random.Range(0f, 0.3f));

        int currentLearning = 0;
        int targetLearning = Mathf.RoundToInt(data.rawLearning);
        int finalScore = targetLearning;

        // 2. SUBIDA DE PUNTOS (Ambas barras suben juntas)
        while (currentLearning < targetLearning)
        {
            currentLearning++;
            learningText.text = currentLearning.ToString();
            
            if(learningSlider != null) learningSlider.value = currentLearning / 100f;
            if(stressSlider != null) stressSlider.value = currentLearning / 100f; // La roja persigue a la verde
            
            yield return new WaitForSeconds(countSpeed);
        }

        yield return new WaitForSeconds(0.5f);

        // 3. ESTRÉS Y PENALIZACIÓN
        stressText.color = Color.red;
        stressText.text = $" Estrés: {Mathf.RoundToInt(data.rawStress)}%";
        
        if (data.rawStress >= 80f)
        {
            if (data.penaltyMode == ExamPenaltyMode.PanicAttack)
            {
                stressText.color = Color.red;
                penaltyText.color = Color.red;
                penaltyText.text = "Castigo: -20 pts";
                
                yield return new WaitForSeconds(0.5f);

                int penaltyTarget = targetLearning - 20;
                learningText.color = Color.red; 

                // ¡EL EFECTO BLOODBORNE!
                // 1. La barra verde baja de un solo golpe, revelando el daño en rojo detrás
                if(learningSlider != null) learningSlider.value = penaltyTarget / 100f;
                
                // Un micro-segundo de pausa para que el jugador sufra viendo el cacho rojo
                yield return new WaitForSeconds(0.3f);
                
                // 2. Ahora la barra roja se drena junto con los números
                while (currentLearning > penaltyTarget)
                {
                    currentLearning--;
                    learningText.text = currentLearning.ToString();
                    
                    if(stressSlider != null) stressSlider.value = currentLearning / 100f;
                    
                    yield return new WaitForSeconds(countSpeed * 2f);
                }
                finalScore = penaltyTarget;
            }
            else if (data.penaltyMode == ExamPenaltyMode.MoneyFine)
            {
                stressText.color = new Color(1f, 0.5f, 0f);
                penaltyText.color = new Color(1f, 0.5f, 0f);
                penaltyText.text = "Multa al Mtro.";
            }
            else if (data.penaltyMode == ExamPenaltyMode.Snowball)
            {
                stressText.color = Color.red;
                penaltyText.text = "¡Burnout!";
            }
        }
        else
        {
            stressText.color = Color.green;
        }

        yield return new WaitForSeconds(0.5f);

        // 4. VEREDICTO
        if (data.isGraduated)
        {
            verdictText.color = Color.yellow;
            verdictText.text = "¡GRADUADO!";
        }
        else if (finalScore >= passingQuota)
        {
            verdictText.color = Color.green;
            verdictText.text = "APROBADO";
        }
        else
        {
            verdictText.color = Color.red;
            verdictText.text = "REPROBADO";
        }
    }
}