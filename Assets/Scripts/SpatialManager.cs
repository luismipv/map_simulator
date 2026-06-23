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

    void Start()
    {
        InvokeRepeating("UpdateSpatialEffects", 1f, 1f);
    }

    void UpdateSpatialEffects()
    {
        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        HashSet<string> parejasActuales = new HashSet<string>();

        // 1. LOS BUSES SEPARADOS: Uno para la Fila (Entorno) y otros para los Vecinos (Sinergia)
        Dictionary<Student, float> tempEntornoLearning = new Dictionary<Student, float>();
        Dictionary<Student, float> tempSinergiaLearning = new Dictionary<Student, float>();
        Dictionary<Student, float> tempSinergiaStress = new Dictionary<Student, float>();

        // Inicializamos todos en neutro (1)
        foreach (Student s in todosLosAlumnos)
        {
            tempEntornoLearning[s] = 1f;
            tempSinergiaLearning[s] = 1f;
            tempSinergiaStress[s] = 1f;
        }

        // 2. CALCULAMOS LA MATEMÁTICA SEPARADA
        for (int i = 0; i < todosLosAlumnos.Length; i++)
        {
            Student s = todosLosAlumnos[i];
            
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // FACTOR 1: Fila -> Se va al bus de Entorno (Ajustado con tu lógica de Y)
            if (s.transform.position.y >= yFilaFrente) //Estás enfrente
            {
                if(s.personalityData.personalityType == StudentPersonality.Nerd)
                {
                    tempEntornoLearning[s] *= 1.2f;
                    Debug.Log("El Nerd está enfrente");
                }
                else if(s.personalityData.personalityType == StudentPersonality.Normal)
                {
                    tempEntornoLearning[s] *= 1f;
                }
                else
                {
                    tempEntornoLearning[s] *= 0.8f;
                }
                
            }
            else if (s.transform.position.y <= yFilaAtras) //Estás atrás
            {
                if(s.personalityData.personalityType == StudentPersonality.Slacker || s.personalityData.personalityType == StudentPersonality.Anxious ){
                    tempEntornoLearning[s] *= 1.2f;
                }
                else if(s.personalityData.personalityType == StudentPersonality.Nerd)
                {
                    tempEntornoLearning[s] *= 0.8f;
                    Debug.Log("El Nerd está atrás");
                }
                else
                {
                    tempEntornoLearning[s] *= 1f;
                }
            } 

            // FACTOR 2: Revisar Vecinos -> Se va a los buses de Sinergia
            for (int j = i + 1; j < todosLosAlumnos.Length; j++) 
            {
                Student vecino = todosLosAlumnos[j];
                if (vecino.currentState == StudentState.DroppedOut || vecino.currentState == StudentState.Graduated) continue;

                float distancia = Vector2.Distance(s.transform.position, vecino.transform.position);

                if (distancia <= radioVecinos)
                {
                    // Le pasamos los diccionarios de sinergia específicos
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

    // Actualizamos los parámetros que recibe para que use los nuevos diccionarios
    void ApplySynergy(Student me, Student neighbor, HashSet<string> parejasActuales, Dictionary<Student, float> synLearn, Dictionary<Student, float> synStress)
    {
        int id1 = me.GetInstanceID();
        int id2 = neighbor.GetInstanceID();
        string pairHash = Mathf.Min(id1, id2).ToString() + "_" + Mathf.Max(id1, id2).ToString();

        parejasActuales.Add(pairHash);

        bool huboSinergia = false;
        bool esPositiva = false;

        // Nerd + Nerd = ¡Bonus de Aprendizaje!
        if (me.personalityData.personalityType == StudentPersonality.Nerd && 
            neighbor.personalityData.personalityType == StudentPersonality.Nerd)
        {
            synLearn[me] *= 1.05f; 
            synLearn[neighbor] *= 1.05f; 
            huboSinergia = true; esPositiva = true;
        }
        
        // Nerd + Slacker = ¡Estrés para el Nerd!
        if ((me.personalityData.personalityType == StudentPersonality.Nerd && neighbor.personalityData.personalityType == StudentPersonality.Slacker) ||
            (neighbor.personalityData.personalityType == StudentPersonality.Nerd && me.personalityData.personalityType == StudentPersonality.Slacker))
        {
            Student elNerd = (me.personalityData.personalityType == StudentPersonality.Nerd) ? me : neighbor;
            synStress[elNerd] *= 1.2f; 
            huboSinergia = true; esPositiva = false;
        }

        if (huboSinergia && !parejasMostradas.Contains(pairHash))
        {
            StartCoroutine(ShowFeedback(me, neighbor, esPositiva));
            parejasMostradas.Add(pairHash);
        }
    }

    private IEnumerator ShowFeedback(Student student, Student neighbor, bool isPositive)
    {
        // Chaleco antibalas para el Feedback visual
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