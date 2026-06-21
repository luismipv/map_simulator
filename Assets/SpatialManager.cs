using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SpatialManager : MonoBehaviour
{
    [Header("Configuración del Salón")]
    public float radioVecinos = 3.5f; // Distancia máxima para considerarse "juntos" (Ajústala en el Inspector)
    public float yFilaFrente = 1.0f;  // Si el alumno está por encima de este valor 'Y', está hasta adelante
    public float yFilaAtras = -1.0f;  // Si está por debajo de este valor 'Y', está hasta atrás

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
        // 1. Buscamos a TODOS los alumnos vivos en el salón de un solo golpe
        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        HashSet<string> parejasActuales = new HashSet<string>();

        // 2. Revisamos alumno por alumno
        for (int i = 0; i < todosLosAlumnos.Length; i++)
        {
            Student s = todosLosAlumnos[i];
            
            // Si ya no está en clase, lo ignoramos
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // RESET de multiplicadores
            s.stressMultiplier = 1f;
            s.learningMultiplier = 1f;

            // FACTOR 1: Fila (Evaluamos simplemente su posición física)
            if (s.transform.position.y >= yFilaFrente) s.learningMultiplier *= 1.2f; 
            else if (s.transform.position.y <= yFilaAtras) s.learningMultiplier *= 0.8f;

            // FACTOR 2: Revisar Vecinos por DISTANCIA
            // Iniciamos "j = i + 1" para no comparar a la misma pareja dos veces (ej. Ana-Luis y luego Luis-Ana)
            for (int j = i + 1; j < todosLosAlumnos.Length; j++) 
            {
                Student vecino = todosLosAlumnos[j];
                if (vecino.currentState == StudentState.DroppedOut || vecino.currentState == StudentState.Graduated) continue;

                // LA MAGIA: Medimos la distancia real entre ellos
                float distancia = Vector2.Distance(s.transform.position, vecino.transform.position);

                if (distancia <= radioVecinos)
                {
                    ApplySynergy(s, vecino, parejasActuales);
                }
            }
        }

        // Limpieza de memoria visual
        parejasMostradas.IntersectWith(parejasActuales);
    }

    void ApplySynergy(Student me, Student neighbor, HashSet<string> parejasActuales)
    {
        // Creamos el Gafete único (Ej. "1456_8943")
        int id1 = me.GetInstanceID();
        int id2 = neighbor.GetInstanceID();
        string pairHash = Mathf.Min(id1, id2).ToString() + "_" + Mathf.Max(id1, id2).ToString();

        parejasActuales.Add(pairHash);

        bool huboSinergia = false;
        bool esPositiva = false;

        // Nerd + Nerd = ¡Bonus!
        if (me.personalityData.personalityType == StudentPersonality.Nerd && 
            neighbor.personalityData.personalityType == StudentPersonality.Nerd)
        {
            me.learningMultiplier *= 1.05f; 
            neighbor.learningMultiplier *= 1.05f; // No olvides darle el boost al vecino también
            huboSinergia = true; esPositiva = true;
        }
        
        // Nerd + Slacker = ¡Estrés para el Nerd!
        if ((me.personalityData.personalityType == StudentPersonality.Nerd && neighbor.personalityData.personalityType == StudentPersonality.Slacker) ||
            (neighbor.personalityData.personalityType == StudentPersonality.Nerd && me.personalityData.personalityType == StudentPersonality.Slacker))
        {
            // Detectamos quién es el Nerd para estresarlo solo a él
            Student elNerd = (me.personalityData.personalityType == StudentPersonality.Nerd) ? me : neighbor;
            elNerd.stressMultiplier *= 1.2f; 
            huboSinergia = true; esPositiva = false;
        }

        // Disparamos Feedback si aplica
        if (huboSinergia && !parejasMostradas.Contains(pairHash))
        {
            StartCoroutine(ShowFeedback(me, neighbor, esPositiva));
            parejasMostradas.Add(pairHash);
        }
    }

    private IEnumerator ShowFeedback(Student student, Student neighbor, bool isPositive)
    {
        GameObject feedbackObj = new GameObject("Feedback Sinergia");
        feedbackObj.transform.position = (student.transform.position + neighbor.transform.position) / 2f; 
        
        SpriteRenderer sr = feedbackObj.AddComponent<SpriteRenderer>();
        sr.sprite = isPositive ? positiveFeedback : negativeFeedback;
        sr.sortingOrder = 10; 

        yield return new WaitForSeconds(1f); 

        Destroy(feedbackObj);
    }

        // Añadimos la visión de desarrollador para el SpatialManager
    private void OnDrawGizmos()
    {
        // Solo lo dibujamos si seleccionamos el objeto en la jerarquía para no ensuciar la pantalla
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // Un verde transparente

        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        if (todosLosAlumnos == null) return;

        foreach (Student s in todosLosAlumnos)
        {
            if (s.currentState != StudentState.DroppedOut && s.currentState != StudentState.Graduated)
            {
                // Dibuja un círculo mostrando exactamente hasta dónde llega la "sinergia" de este alumno
                Gizmos.DrawWireSphere(s.transform.position, radioVecinos);
            }
        }
    }
}