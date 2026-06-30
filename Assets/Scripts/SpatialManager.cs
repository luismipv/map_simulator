using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SpatialManager : MonoBehaviour
{
    [Header("Configuración del Salón")]
    public float radioVecinos = 3.5f; 
    public float yFilaFrente = -1.0f;  
    public float yFilaAtras = 1.0f;  

    [Header("Feedback Visual")]
    public Sprite positiveFeedback; 
    public Sprite negativeFeedback; 
    
    private HashSet<string> parejasMostradas = new HashSet<string>();
    public List<SynergyRuleSO> reglasDeSinergia = new List<SynergyRuleSO>();

    void Start()
    {
        InvokeRepeating("UpdateSpatialEffects", 1f, 1f);
    }

    void UpdateSpatialEffects()
    {
        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        HashSet<string> parejasActuales = new HashSet<string>();

        Dictionary<Student, float> tempEntornoLearning = new Dictionary<Student, float>();
        Dictionary<Student, float> tempSinergiaLearning = new Dictionary<Student, float>();
        Dictionary<Student, float> tempSinergiaStress = new Dictionary<Student, float>();

        foreach (Student s in todosLosAlumnos)
        {
            tempEntornoLearning[s] = 1f;
            tempSinergiaLearning[s] = 1f;
            tempSinergiaStress[s] = 1f;
        }

        for (int i = 0; i < todosLosAlumnos.Length; i++)
        {
            Student s = todosLosAlumnos[i];
            
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;
            
            // CHALECO ANTIBALAS 1: Si este alumno no tiene personalidad, lo saltamos para no romper el juego
            if (s.personalityData == null) continue;

            // FACTOR 1: Fila -> Se va al bus de Entorno
            if (s.transform.position.y >= yFilaFrente) // Estás enfrente
            {
                if(s.personalityData.personalityType == StudentPersonality.Nerd)
                {
                    tempEntornoLearning[s] *= 1.2f;
                }
                else if(s.personalityData.personalityType == StudentPersonality.Normal || s.personalityData.personalityType == StudentPersonality.Cool)
                {
                    tempEntornoLearning[s] *= 1f;
                }
               
                else
                {
                    tempEntornoLearning[s] *= 0.8f;
                }
            }
            else if (s.transform.position.y <= yFilaAtras) // Estás atrás
            {
                if(s.personalityData.personalityType == StudentPersonality.Slacker || s.personalityData.personalityType == StudentPersonality.Anxious || s.personalityData.personalityType == StudentPersonality.Bully )
                {
                    tempEntornoLearning[s] *= 1.2f;
                }
                else if(s.personalityData.personalityType == StudentPersonality.Nerd)
                {
                    tempEntornoLearning[s] *= 0.8f;
                }
                else
                {
                    tempEntornoLearning[s] *= 1f;
                }
            } 

            // FACTOR 2: Revisar Vecinos
            for (int j = i + 1; j < todosLosAlumnos.Length; j++) 
            {
                Student vecino = todosLosAlumnos[j];
                if (vecino.currentState == StudentState.DroppedOut || vecino.currentState == StudentState.Graduated) continue;
                
                // CHALECO ANTIBALAS 2: Revisamos que el vecino también tenga personalidad
                if (vecino.personalityData == null) continue;

                float distancia = Vector2.Distance(s.transform.position, vecino.transform.position);

                if (distancia <= radioVecinos)
                {
                    ApplySynergy(s, vecino, parejasActuales, tempSinergiaLearning, tempSinergiaStress);
                }
            }
        }

        // 3. ¡INYECTAMOS LAS ETIQUETAS INDEPENDIENTES!
        foreach (Student s in todosLosAlumnos)
        {
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // A) Etiqueta de Entorno (Fila)
            if (tempEntornoLearning[s] != 1f) s.activeLearningBuffs["Entorno 🧠"] = tempEntornoLearning[s];
            else s.activeLearningBuffs.Remove("Entorno 🧠");

            // B) Etiqueta de Sinergia (Vecinos - Aprendizaje)
            if (tempSinergiaLearning[s] != 1f) s.activeLearningBuffs["Sinergia 🧠"] = tempSinergiaLearning[s];
            else s.activeLearningBuffs.Remove("Sinergia 🧠");

            // C) Etiqueta de Sinergia (Vecinos - Estrés)
            if (tempSinergiaStress[s] != 1f) s.activeStressBuffs["Sinergia 💢"] = tempSinergiaStress[s];
            else s.activeStressBuffs.Remove("Sinergia 💢");
        }

        parejasMostradas.IntersectWith(parejasActuales);
    }

    void ApplySynergy(Student me, Student neighbor, HashSet<string> parejasActuales, Dictionary<Student, float> synLearn, Dictionary<Student, float> synStress)
    {
        int id1 = me.GetInstanceID();
        int id2 = neighbor.GetInstanceID();
        string pairHash = Mathf.Min(id1, id2).ToString() + "_" + Mathf.Max(id1, id2).ToString();

        parejasActuales.Add(pairHash);

        StudentPersonality myType = me.personalityData.personalityType;
        StudentPersonality neighborType = neighbor.personalityData.personalityType;

        SynergyRuleSO reglaValida = null;
        
        // CHALECO ANTIBALAS 3: Evitamos errores si la lista en Unity está vacía
        if (reglasDeSinergia != null && reglasDeSinergia.Count > 0)
        {
            foreach (SynergyRuleSO regla in reglasDeSinergia)
            {
                if (regla != null && regla.Matches(myType, neighborType))
                {
                    reglaValida = regla;
                    break;
                }
            }
        }

        if (reglaValida != null)
        {
            bool huboSinergia = false;
            bool esPositiva = false; 

            // CASO 1: Yo soy A y vecino es B
            if (reglaValida.personalityA == myType && reglaValida.personalityB == neighborType)
            {
                synLearn[me] *= reglaValida.learningMultA;
                synStress[me] *= reglaValida.stressMultA;

                synLearn[neighbor] *= reglaValida.learningMultB;
                synStress[neighbor] *= reglaValida.stressMultB;
                
                huboSinergia = true;
                esPositiva = (reglaValida.learningMultA >= 1f);
            }
            // CASO 2: Yo soy B y vecino es A (Invertido)
            else if (reglaValida.personalityA == neighborType && reglaValida.personalityB == myType)
            {
                synLearn[me] *= reglaValida.learningMultB;
                synStress[me] *= reglaValida.stressMultB;

                synLearn[neighbor] *= reglaValida.learningMultA;
                synStress[neighbor] *= reglaValida.stressMultA;
                
                huboSinergia = true;
                esPositiva = (reglaValida.learningMultB >= 1f);
            }

            if (huboSinergia && !parejasMostradas.Contains(pairHash))
            {
                StartCoroutine(ShowFeedback(me, neighbor, esPositiva));
                parejasMostradas.Add(pairHash);
            }
        }
    }

    private IEnumerator ShowFeedback(Student student, Student neighbor, bool isPositive)
    {
        if (student == null || neighbor == null) yield break;

        GameObject feedbackObj = new GameObject("Feedback Sinergia");
        feedbackObj.transform.position = (student.transform.position + neighbor.transform.position) / 2f; 
        
        SpriteRenderer sr = feedbackObj.AddComponent<SpriteRenderer>();
        sr.sprite = isPositive ? positiveFeedback : negativeFeedback;
        sr.sortingOrder = 10; 

        yield return new WaitForSeconds(1f); 

        if (feedbackObj != null) Destroy(feedbackObj);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f); 

        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        if (todosLosAlumnos == null) return;

        foreach (Student s in todosLosAlumnos)
        {
            if (s.currentState != StudentState.DroppedOut && s.currentState != StudentState.Graduated)
            {
                Gizmos.DrawWireSphere(s.transform.position, radioVecinos);
            }
        }
    }
}