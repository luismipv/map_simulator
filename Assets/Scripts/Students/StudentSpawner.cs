using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StudentSpawner : MonoBehaviour
{
    [Header("Configuración Base")]
    public GameObject studentPrefab;
    
    [Header("Base de Datos (Para el Random)")]
    public StudentPersonalitySO[] availablePersonalities; 
    public string[] firstNames = new string[] { "Ana", "Luis", "Sofía", "Carlos", "Elena", "Marta", "Diego" };
    public string[] lastNames = new string[] { "Pérez", "Gómez", "López", "Martínez", "Rodríguez", "Sánchez" };

    [Header("3D Perspective")]
    public StudentAspectManager studentAspectManager;

    // Control Interno
    private GameObject currentLayoutInstance;
    private Transform seatsContainer;

    void Awake()
    {
        if(studentAspectManager == null)
            studentAspectManager = GetComponent<StudentAspectManager>();
    }

    // ¡EL MÉTODO MAESTRO COMPLETO!
    public List<Student> SpawnStudents(LevelData levelData)
    {
        // 0. Paracaídas por si el nivel llega vacío
        if (levelData == null)
        {
            Debug.LogError("¡LevelData llegó vacío al Spawner!");
            return new List<Student>();
        }

        if (currentLayoutInstance != null) Destroy(currentLayoutInstance);
        
        List<Student> spawnedStudents = new List<Student>();

        // 1. LEEMOS EL PAQUETE COMPLETO DEL LAYOUT Y CONSTRUIMOS EL SALÓN
        if (levelData.classroomLayout != null && levelData.classroomLayout.layoutPrefab != null)
        {
            currentLayoutInstance = Instantiate(levelData.classroomLayout.layoutPrefab, Vector3.zero, Quaternion.identity);
            seatsContainer = currentLayoutInstance.transform;

            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetCameraTarget(
                    levelData.classroomLayout.idealCameraPosition, 
                    levelData.classroomLayout.idealCameraSize
                );
            }
        }
        else
        {
            Debug.LogError("¡Falta asignar el LayoutData en tu Nivel! No se pueden instanciar alumnos.");
            return spawnedStudents; // Salimos a salvo sin crashear
        }
        
        // 2. RECOLECTAR LAS SILLAS DEL NUEVO SALÓN
        Seat[] allSeats = seatsContainer.GetComponentsInChildren<Seat>();
        List<Seat> availableSeats = new List<Seat>();
        foreach (Seat s in allSeats)
        {
            if (s.currentStudent == null) availableSeats.Add(s);
        }

        // 3. DEFINIR LA LISTA DE ALUMNOS (RANDOM VS FIJA)
        List<StudentPersonalitySO> rosterToSpawn = new List<StudentPersonalitySO>();

        if (levelData.spawnMode == SpawnMode.RandomWithWeights)
        {
            for (int i = 0; i < levelData.totalRandomStudents; i++)
            {
                rosterToSpawn.Add(GetRandomPersonalityByWeight());
            }
        }
        else // SpawnMode.FixedList
        {
            if (levelData.fixedStudentRoster != null)
                rosterToSpawn.AddRange(levelData.fixedStudentRoster);
        }

        // 4. ¡A SPAWNEAR!
        int studentsToSpawn = Mathf.Min(rosterToSpawn.Count, availableSeats.Count);
        GameObject prefabToUse = (levelData.tutorialStudentPrefabOverride != null) ? levelData.tutorialStudentPrefabOverride : studentPrefab;

        for (int i = 0; i < studentsToSpawn; i++)
        {
            Seat chosenSeat;

            // Orden o Aleatorio
            if (levelData.spawnMode == SpawnMode.RandomWithWeights)
            {
                int randomIndex = Random.Range(0, availableSeats.Count);
                chosenSeat = availableSeats[randomIndex];
                availableSeats.RemoveAt(randomIndex);
            }
            else
            {
                chosenSeat = availableSeats[0];
                availableSeats.RemoveAt(0);
            }

            // Crear alumno
            GameObject newStudentObj = Instantiate(prefabToUse, transform.position, Quaternion.identity);
            Student studentScript = newStudentObj.GetComponent<Student>();

            chosenSeat.AssignStudent(studentScript);

            string generatedName = GenerateRandomName();
            newStudentObj.name = generatedName;
            studentScript.studentName = generatedName;
            studentScript.personalityData = rosterToSpawn[i];

            TMP_Text textUI = newStudentObj.GetComponentInChildren<TMP_Text>();
            if (textUI != null) textUI.text = $"{generatedName}:\n0/100";
            
            // 3D Model
            if (studentAspectManager != null)
                studentAspectManager.GenerateStudentRandomAppearance(newStudentObj, studentScript.personalityData.personalityType);

            // ¡GUARDAMOS AL ALUMNO EN LA LISTA ANTES DE SEGUIR!
            spawnedStudents.Add(studentScript);
        }
        
        return spawnedStudents;
    }

    private StudentPersonalitySO GetRandomPersonalityByWeight()
    {
        int totalWeight = 0;
        foreach (var p in availablePersonalities) totalWeight += p.spawnWeight;
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeightSum = 0;
        
        foreach (var p in availablePersonalities)
        {
            currentWeightSum += p.spawnWeight;
            if (randomValue < currentWeightSum) return p; 
        }
        
        if (availablePersonalities.Length > 0) return availablePersonalities[0];
        return null;
    }

    string GenerateRandomName()
    {
        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }
}