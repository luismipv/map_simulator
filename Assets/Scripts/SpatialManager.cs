using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SpatialManager : MonoBehaviour
{
    public static SpatialManager Instance { get; private set; }
    
    [Header("Configuración del Salón (Eje Z)")]
    public float radioVecinos = 3.5f; 
    public float zFilaFrente = 5.0f;  
    public float zFilaAtras = -5.0f;  

    public Dictionary<Student, List<Student>> neighborGraph = new Dictionary<Student, List<Student>>();

    [Header("Feedback Visual (Partículas)")]
    public GameObject positiveParticlesPrefab; 
    public GameObject negativeParticlesPrefab; 
    
    private HashSet<string> parejasMostradas = new HashSet<string>();
    public List<SynergyRuleSO> reglasDeSinergia = new List<SynergyRuleSO>();

    public void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }
    
    void Start()
    {
        UpdateSpatialEffects(true); 
    }

    public void UpdateSpatialEffects(bool spawnParticles = false)
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
            neighborGraph[s] = new List<Student>(); 
        }

        for (int i = 0; i < todosLosAlumnos.Length; i++)
        {
            Student s = todosLosAlumnos[i];
            
            // ¡NUEVO! Candado de vuelo para el radar
            Student3D s3d = s as Student3D;
            if (s3d != null && s3d.IsDragged) continue; 

            if (s.currentState != StudentState.Working && s.currentState != StudentState.Finished && s.currentState != StudentState.Flow) continue;
            if (s.personalityData == null) continue;

            if (s.transform.position.z >= zFilaFrente) 
            {
                if(s.personalityData.personalityType == StudentPersonality.Nerd) tempEntornoLearning[s] *= 1.2f;
                else if(s.personalityData.personalityType == StudentPersonality.Normal || s.personalityData.personalityType == StudentPersonality.Cool) tempEntornoLearning[s] *= 1f;
                else tempEntornoLearning[s] *= 0.8f;
            }
            else if (s.transform.position.z <= zFilaAtras) 
            {
                if(s.personalityData.personalityType == StudentPersonality.Slacker || s.personalityData.personalityType == StudentPersonality.Anxious || s.personalityData.personalityType == StudentPersonality.Bully ) tempEntornoLearning[s] *= 1.2f;
                else if(s.personalityData.personalityType == StudentPersonality.Nerd) tempEntornoLearning[s] *= 0.8f;
                else tempEntornoLearning[s] *= 1f;
            } 

            for (int j = i + 1; j < todosLosAlumnos.Length; j++) 
            {
                Student vecino = todosLosAlumnos[j];
                
                // ¡NUEVO! Candado de vuelo para los vecinos
                Student3D vecino3d = vecino as Student3D;
                if (vecino3d != null && vecino3d.IsDragged) continue;

                if (vecino.currentState != StudentState.Working && vecino.currentState != StudentState.Finished && vecino.currentState != StudentState.Flow) continue;
                if (vecino.personalityData == null) continue;

                Vector3 posS = new Vector3(s.transform.position.x, 0, s.transform.position.z);
                Vector3 posVecino = new Vector3(vecino.transform.position.x, 0, vecino.transform.position.z);
                float distancia = Vector3.Distance(posS, posVecino);

                if (distancia <= radioVecinos)
                {
                    neighborGraph[s].Add(vecino);
                    neighborGraph[vecino].Add(s);
                    
                    bool sActivo = (s.currentState == StudentState.Working || s.currentState == StudentState.Flow);
                    bool vecinoActivo = (vecino.currentState == StudentState.Working || vecino.currentState == StudentState.Flow);
                    
                    if (sActivo && vecinoActivo)
                    {
                        ApplySynergy(s, vecino, parejasActuales, tempSinergiaLearning, tempSinergiaStress, spawnParticles);
                    }
                }
            }
        }

        foreach (Student s in todosLosAlumnos)
        {
            if (s == null || s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            Student3D s3d = s as Student3D;
            bool estaVolando = (s3d != null && s3d.IsDragged);

            bool tieneTutorCerca = false;
            if (neighborGraph.ContainsKey(s))
            {
                foreach (Student vecino in neighborGraph[s])
                {
                    if (vecino.currentState == StudentState.Finished) 
                    {
                        tieneTutorCerca = true;
                        break; 
                    }
                }
            }

            if (tieneTutorCerca) s.AddLearningModifier(ModifierID.Tutor, 1.5f);
            else s.RemoveLearningModifier(ModifierID.Tutor);

            // Solo inyectamos etiquetas si no está volando
            if (!estaVolando && (s.currentState == StudentState.Working || s.currentState == StudentState.Flow))
            {
                if (tempEntornoLearning[s] != 1f) s.AddLearningModifier(ModifierID.Entorno, tempEntornoLearning[s]);
                else s.RemoveLearningModifier(ModifierID.Entorno);

                if (tempSinergiaLearning[s] != 1f) s.AddLearningModifier(ModifierID.Sinergia, tempSinergiaLearning[s]);
                else s.RemoveLearningModifier(ModifierID.Sinergia);

                if (tempSinergiaStress[s] != 1f) s.AddStressModifier(ModifierID.Sinergia, tempSinergiaStress[s]);
                else s.RemoveStressModifier(ModifierID.Sinergia);
            }
            else 
            {
                s.RemoveLearningModifier(ModifierID.Entorno);
                s.RemoveLearningModifier(ModifierID.Sinergia);
                s.RemoveStressModifier(ModifierID.Sinergia);
            }
        }

        parejasMostradas.IntersectWith(parejasActuales);
    }

    void ApplySynergy(Student me, Student neighbor, HashSet<string> parejasActuales, Dictionary<Student, float> synLearn, Dictionary<Student, float> synStress, bool spawnParticles)
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
            bool meEsPositivo = false; 
            bool neighborEsPositivo = false; 

            if (reglaValida.personalityA == myType && reglaValida.personalityB == neighborType)
            {
                synLearn[me] *= reglaValida.learningMultA;
                synStress[me] *= reglaValida.stressMultA;
                synLearn[neighbor] *= reglaValida.learningMultB;
                synStress[neighbor] *= reglaValida.stressMultB;
                
                huboSinergia = true;

                // --- ¡EL NUEVO SISTEMA DE BALANCE! ---
                float scoreMe = (reglaValida.learningMultA - 1f) + (1f - reglaValida.stressMultA);
                meEsPositivo = (scoreMe >= 0f);

                float scoreNeighbor = (reglaValida.learningMultB - 1f) + (1f - reglaValida.stressMultB);
                neighborEsPositivo = (scoreNeighbor >= 0f);
            }
            else if (reglaValida.personalityA == neighborType && reglaValida.personalityB == myType)
            {
                synLearn[me] *= reglaValida.learningMultB;
                synStress[me] *= reglaValida.stressMultB;
                synLearn[neighbor] *= reglaValida.learningMultA;
                synStress[neighbor] *= reglaValida.stressMultA;
                
                huboSinergia = true;

                // --- ¡EL NUEVO SISTEMA DE BALANCE (CASO INVERTIDO)! ---
                float scoreMe = (reglaValida.learningMultB - 1f) + (1f - reglaValida.stressMultB);
                meEsPositivo = (scoreMe >= 0f);

                float scoreNeighbor = (reglaValida.learningMultA - 1f) + (1f - reglaValida.stressMultA);
                neighborEsPositivo = (scoreNeighbor >= 0f);
            }

            if (huboSinergia && !parejasMostradas.Contains(pairHash))
            {
                if (spawnParticles)
                {
                    StartCoroutine(ShowFeedback(me, neighbor, meEsPositivo, neighborEsPositivo));
                }
                parejasMostradas.Add(pairHash);
            }
        }
    }

    private IEnumerator ShowFeedback(Student student, Student neighbor, bool isPositiveMe, bool isPositiveNeighbor)
    {
        if (student == null || neighbor == null) yield break;

        GameObject prefabMe = isPositiveMe ? positiveParticlesPrefab : negativeParticlesPrefab;
        GameObject prefabNeighbor = isPositiveNeighbor ? positiveParticlesPrefab : negativeParticlesPrefab;

        //Sonido de feedback

        if (isPositiveMe) AudioManager.Instance.PostEvent("Synergy_Positive", student.gameObject);
        else AudioManager.Instance.PostEvent("Synergy_Negative", student.gameObject);
        if (isPositiveNeighbor) AudioManager.Instance.PostEvent("Synergy_Positive", neighbor.gameObject);
        else AudioManager.Instance.PostEvent("Synergy_Negative", neighbor.gameObject);
        
        /////////---------------------

        float alturaOffset = 3.5f; 
        float zOffset = -1.0f; 

        if (prefabMe != null)
        {
            Vector3 posStudent = student.transform.position;
            posStudent.y += alturaOffset;
            posStudent.z += zOffset;
            GameObject part1 = Instantiate(prefabMe, posStudent, Quaternion.identity);
            part1.transform.SetParent(student.transform); 
            Destroy(part1, 2f);
        }

        if (prefabNeighbor != null)
        {
            Vector3 posNeighbor = neighbor.transform.position;
            posNeighbor.y += alturaOffset;
            posNeighbor.z += zOffset;
            GameObject part2 = Instantiate(prefabNeighbor, posNeighbor, Quaternion.identity);
            part2.transform.SetParent(neighbor.transform); 
            Destroy(part2, 2f);
        }

        yield return null; 
    }

    //... OnDrawGizmos se queda igual
   private void OnDrawGizmos()
    {
        // 1. DIBUJAMOS LA BRÚJULA DEL SALÓN (Frente y Atrás)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Amarillo semitransparente
        Vector3 size = new Vector3(20f, 0.1f, 0.5f); // Un rectángulo largo que cruza el salón
        
        Gizmos.DrawCube(new Vector3(0, 0, zFilaFrente), size); // Línea de los Nerds
        Gizmos.DrawCube(new Vector3(0, 0, zFilaAtras), size);  // Línea de los Slackers

        Student[] todosLosAlumnos = FindObjectsByType<Student>(FindObjectsSortMode.None);
        if (todosLosAlumnos == null) return;

        // 2. DIBUJAMOS EL RADAR INTELIGENTE
        foreach (Student s in todosLosAlumnos)
        {
            if (s.currentState == StudentState.DroppedOut || s.currentState == StudentState.Graduated) continue;

            // Verificamos si el alumno tiene vecinos conectados en su red
            bool tieneVecinos = neighborGraph != null && neighborGraph.ContainsKey(s) && neighborGraph[s].Count > 0;
            
            // Si está conectado es Verde, si está aislado es Blanco fantasma
            Gizmos.color = tieneVecinos ? Color.green : new Color(1f, 1f, 1f, 0.2f);

            // Lo aplanamos al piso para que la esfera no flote raro si el modelo es muy alto
            Vector3 pisoPos = new Vector3(s.transform.position.x, 0, s.transform.position.z);
            Gizmos.DrawWireSphere(pisoPos, radioVecinos);
        }
    }
}