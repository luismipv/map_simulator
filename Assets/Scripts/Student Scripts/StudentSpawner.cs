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

    // ¡EL NUEVO MÉTODO MAESTRO!
    public void SpawnStudents(LevelData levelData)
    {
       if (currentLayoutInstance != null) Destroy(currentLayoutInstance);
        
        // 1. LEEMOS EL PAQUETE COMPLETO DEL LAYOUT
        if (levelData.classroomLayout != null && levelData.classroomLayout.layoutPrefab != null)
        {
            // Construimos el salón
            currentLayoutInstance = Instantiate(levelData.classroomLayout.layoutPrefab, Vector3.zero, Quaternion.identity);
            seatsContainer = currentLayoutInstance.transform;

            // Ajustamos la cámara
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
            Debug.LogError("¡Falta asignar el LayoutData en tu Nivel!");
            return;
        }
        

        // 3. RECOLECTAR LAS SILLAS DEL NUEVO SALÓN
        Seat[] allSeats = seatsContainer.GetComponentsInChildren<Seat>();
        List<Seat> availableSeats = new List<Seat>();
        foreach (Seat s in allSeats)
        {
            if (s.currentStudent == null) availableSeats.Add(s);
        }

        // 4. DEFINIR LA LISTA DE ALUMNOS (RANDOM VS FIJA)
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
            rosterToSpawn.AddRange(levelData.fixedStudentRoster);
        }

        // 5. ¡A SPAWNEAR! (Límite: No más alumnos que sillas)
        int studentsToSpawn = Mathf.Min(rosterToSpawn.Count, availableSeats.Count);

        for (int i = 0; i < studentsToSpawn; i++)
        {
            Seat chosenSeat;

            // --- ¡LA MAGIA DEL ORDEN! ---
            if (levelData.spawnMode == SpawnMode.RandomWithWeights)
            {
                // Silla aleatoria
                int randomIndex = Random.Range(0, availableSeats.Count);
                chosenSeat = availableSeats[randomIndex];
                availableSeats.RemoveAt(randomIndex);
            }
            else
            {
                // Orden estricto: Siempre tomamos la primera silla de la lista y la removemos
                chosenSeat = availableSeats[0];
                availableSeats.RemoveAt(0);
            }
            // ----------------------------

            // Crear alumno
            GameObject newStudentObj = Instantiate(studentPrefab, transform.position, Quaternion.identity);
            Student studentScript = newStudentObj.GetComponent<Student>();

            chosenSeat.AssignStudent(studentScript);

            string generatedName = GenerateRandomName();
            newStudentObj.name = generatedName;
            studentScript.studentName = generatedName;

            // ASIGNAR PERSONALIDAD SEGÚN LA LISTA QUE ARMAMOS
            studentScript.personalityData = rosterToSpawn[i];

            TMP_Text textUI = newStudentObj.GetComponentInChildren<TMP_Text>();
            if (textUI != null) textUI.text = $"{generatedName}:\n0/100";
            
            // 3D Model
            studentAspectManager.GenerateStudentRandomAppearance(newStudentObj, studentScript.personalityData.personalityType);
        }
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
        
        return availablePersonalities[0]; 
    }

    string GenerateRandomName()
    {
        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }
}