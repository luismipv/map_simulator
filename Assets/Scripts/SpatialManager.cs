using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SpatialManager : MonoBehaviour
{
    public static SpatialManager Instance { get; private set; }
    
    [Header("Configuración del Salón (Eje Z)")]
    public float radioVecinos = 3.5f; 
    public float zFilaFrente = 5.0f;  // Ajusta en el inspector de Unity
    public float zFilaAtras = -5.0f;  // Ajusta en el inspector de Unity

    public Dictionary<Student, List<Student>> neighborGraph = new Dictionary<Student, List<Student>>();

    [Header("Feedback Visual (Partículas)")]
    public GameObject positiveParticlesPrefab; // Arrastra tu sistema de Corazones aquí
    public GameObject negativeParticlesPrefab; // Arrastra tus partículas de estrés aquí
    
    private HashSet<string> parejasMostradas = new HashSet<string>();
    public List<SynergyRuleSO> reglasDeSinergia = new List<SynergyRuleSO>();

    public void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }
    
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

        neighborGraph.Clear();
        foreach (Student s in todosLosAlumnos)
        {
            neighborGraph[s] = new List<Student>(); // Le creamos una lista vacía a cada quien
        }

        for (int i = 0; i < todosLosAlumnos.Length; i++)
        {
            Student s = todosLosAlumnos[i];
            
            // --- EL CANDADO MAESTRO ---
            // Si el alumno no está sentado trabajando (ej. está siendo arrastrado o dado de baja), 
            // no evaluamos su sinergia ni su posición en la fila.
            if (s.currentState != StudentState.Working) continue;
            
            if (s.personalityData == null) continue;

            // FACTOR 1: Fila -> Se va al bus de Entorno (AQUÍ YA USAMOS LA Z)
            if (s.transform.position.z >= zFilaFrente) 
            {
                if(s.personalityData.personalityType == StudentPersonality.Nerd)
                    tempEntornoLearning[s] *= 1.2f;
                else if(s.personalityData.personalityType == StudentPersonality.Normal || s.personalityData.personalityType == StudentPersonality.Cool)
                    tempEntornoLearning[s] *= 1f;
                else
                    tempEntornoLearning[s] *= 0.8f;
            }
            else if (s.transform.position.z <= zFilaAtras) 
            {
                if(s.personalityData.personalityType == StudentPersonality.Slacker || s.personalityData.personalityType == StudentPersonality.Anxious || s.personalityData.personalityType == StudentPersonality.Bully )
                    tempEntornoLearning[s] *= 1.2f;
                else if(s.personalityData.personalityType == StudentPersonality.Nerd)
                    tempEntornoLearning[s] *= 0.8f;
                else
                    tempEntornoLearning[s] *= 1f;
            } 

            // FACTOR 2: Revisar Vecinos
            for (int j = i + 1; j < todosLosAlumnos.Length; j++) 
            {
                Student vecino = todosLosAlumnos[j];
                
                // Si el vecino está volando, tampoco hay sinergia
                if (vecino.currentState != StudentState.Working) continue;
                if (vecino.personalityData == null) continue;

                // Aplanamos las coordenadas (Ignoramos la altura Y) para medir la distancia real en el piso
                Vector3 posS = new Vector3(s.transform.position.x, 0, s.transform.position.z);
                Vector3 posVecino = new Vector3(vecino.transform.position.x, 0, vecino.transform.position.z);
                float distancia = Vector3.Distance(posS, posVecino);

                if (distancia <= radioVecinos)
                {
                    neighborGraph[s].Add(vecino);
                    neighborGraph[vecino].Add(s);
                    ApplySynergy(s, vecino, parejasActuales, tempSinergiaLearning, tempSinergiaStress);
                }
            }
        }

        // 3. ¡INYECTAMOS LAS ETIQUETAS INDEPENDIENTES!
        foreach (Student s in todosLosAlumnos)
        {
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // Si está trabajando, inyectamos los buffs
            if (s.currentState == StudentState.Working)
            {
                if (tempEntornoLearning[s] != 1f) s.activeLearningBuffs["Entorno 🧠"] = tempEntornoLearning[s];
                else s.activeLearningBuffs.Remove("Entorno 🧠");

                if (tempSinergiaLearning[s] != 1f) s.activeLearningBuffs["Sinergia 🧠"] = tempSinergiaLearning[s];
                else s.activeLearningBuffs.Remove("Sinergia 🧠");

                if (tempSinergiaStress[s] != 1f) s.activeStressBuffs["Sinergia 💢"] = tempSinergiaStress[s];
                else s.activeStressBuffs.Remove("Sinergia 💢");
            }
            else 
            {
                // Si lo levantaste con el dedo, le quitamos las etiquetas temporalmente
                s.activeLearningBuffs.Remove("Entorno 🧠");
                s.activeLearningBuffs.Remove("Sinergia 🧠");
                s.activeStressBuffs.Remove("Sinergia 💢");
            }
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

            if (reglaValida.personalityA == myType && reglaValida.personalityB == neighborType)
            {
                synLearn[me] *= reglaValida.learningMultA;
                synStress[me] *= reglaValida.stressMultA;

                synLearn[neighbor] *= reglaValida.learningMultB;
                synStress[neighbor] *= reglaValida.stressMultB;
                
                huboSinergia = true;
                esPositiva = (reglaValida.learningMultA >= 1f);
            }
            else if (reglaValida.personalityA == neighborType && reglaValida.personalityB == myType)
            {
                synLearn[me] *= reglaValida.learningMultB;
                synStress[me] *= reglaValida.stressMultB;

                synLearn[neighbor] *= reglaValida.learningMultA;
                synStress[neighbor] *= reglaValida.stressMultA;
                
                huboSinergia = true;
                esPositiva = (reglaValida.learningMultB >= 1f);
            }

            // Ya comprobamos arriba que AMBOS están sentados (Working), así que explotamos las partículas de inmediato
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

        GameObject prefabAUsar = isPositive ? positiveParticlesPrefab : negativeParticlesPrefab;

        if (prefabAUsar != null)
        {
            // Subimos más la altura y empujamos las partículas hacia la cámara
            float alturaOffset = 3.5f; 
            float zOffset = -1.0f; // Este -1 lo saca de la cabeza hacia el frente

            // 1. Partículas pegadas al primer alumno
            Vector3 posStudent = student.transform.position;
            posStudent.y += alturaOffset;
            posStudent.z += zOffset;
            GameObject part1 = Instantiate(prefabAUsar, posStudent, Quaternion.identity);
            part1.transform.SetParent(student.transform); 

            // 2. Partículas pegadas al vecino
            Vector3 posNeighbor = neighbor.transform.position;
            posNeighbor.y += alturaOffset;
            posNeighbor.z += zOffset;
            GameObject part2 = Instantiate(prefabAUsar, posNeighbor, Quaternion.identity);
            part2.transform.SetParent(neighbor.transform); 

            Destroy(part1, 2f);
            Destroy(part2, 2f);
        }

        yield return null; 
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
                // Aplanamos el radar visual para que no te mienta en la vista de edición
                Vector3 pisoPos = new Vector3(s.transform.position.x, 0, s.transform.position.z);
                Gizmos.DrawWireSphere(pisoPos, radioVecinos);
            }
        }
    }
}