using UnityEngine;
using System.Collections.Generic;
using TMPro;

// 1. ESTA CAJITA DE DATOS VA AFUERA PARA PODER CONFIGURAR TODO EN EL INSPECTOR
[System.Serializable]
public class LayoutConfig
{
    public string layoutName; 
    public GameObject layoutContainer;
    public Vector3 idealCameraPosition;
    public float idealCameraSize = 5f; 
}

public class StudentSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject studentPrefab;
    public Transform seatsContainer; // Se actualizará automáticamente al cambiar de layout
    public int numberOfStudents = 4; 

    [Header("Base de Datos")]
    public StudentPersonalitySO[] availablePersonalities; 
    public string[] firstNames = new string[] { "Ana", "Luis", "Sofía", "Carlos", "Elena", "Marta", "Diego" };
    public string[] lastNames = new string[] { "Pérez", "Gómez", "López", "Martínez", "Rodríguez", "Sánchez" };

    // ¡NUEVO!: Lista de layouts configurables desde Unity
    [Header("Layouts del Salón")]
    public LayoutConfig[] classroomLayouts;

    [Header("3D Perspective")]
    public StudentAspectManager studentAspectManager;

    void Awake()
    {
        if(studentAspectManager == null)
        {
            studentAspectManager = GetComponent<StudentAspectManager>();
        }
    }

    void Start()
    {
        SetStudentCountFromSlider(4);
        SelectClassroomLayout(0);
       
        //numberOfStudents = (int)UIManager.Instance.studentCountSlider.value;
        UIManager.Instance.UpdateStudentCountText(numberOfStudents);
    }

    public void SetStudentCountFromSlider(float count)
    {
        numberOfStudents = Mathf.RoundToInt(count);
        UIManager.Instance.UpdateStudentCountText(numberOfStudents);
    }

    // ¡NUEVO!: Este es el método que llamará tu botón o dropdown del menú
    public void SelectClassroomLayout(int layoutIndex)
    {
        if (classroomLayouts == null || classroomLayouts.Length == 0) return;

        // A. Prendemos el layout seleccionado y apagamos los demás
        for (int i = 0; i < classroomLayouts.Length; i++)
        {
            if (classroomLayouts[i].layoutContainer != null)
            {
                classroomLayouts[i].layoutContainer.SetActive(i == layoutIndex);
            }
        }

        // B. Le reasignamos el contenedor de sillas al Spawner
        seatsContainer = classroomLayouts[layoutIndex].layoutContainer.transform;
        
        // C. Le delegamos la orden a la cámara de forma limpia (con el script que desacoplamos)
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetCameraTarget(
                classroomLayouts[layoutIndex].idealCameraPosition, 
                classroomLayouts[layoutIndex].idealCameraSize
            );
        }
    }

    public void SpawnStudentsInSeats()
    {
        // 1. Obtenemos todas las sillas disponibles
        Seat[] allSeats = seatsContainer.GetComponentsInChildren<Seat>();
        List<Seat> availableSeats = new List<Seat>();

        foreach (Seat s in allSeats)
        {
            if (s.currentStudent == null)
            {
                availableSeats.Add(s);
            }
        }

        // 2. Limite de seguridad: No podemos spawnear más alumnos que sillas disponibles
        int studentsToSpawn = Mathf.Min(numberOfStudents, availableSeats.Count);

        for (int i = 0; i < studentsToSpawn; i++)
        {
            int randomIndex = Random.Range(0, availableSeats.Count);
            Seat chosenSeat = availableSeats[randomIndex];
            availableSeats.RemoveAt(randomIndex);

            GameObject newStudentObj = Instantiate(studentPrefab, transform.position, Quaternion.identity);
            Student studentScript = newStudentObj.GetComponent<Student>();


            chosenSeat.AssignStudent(studentScript);

            string generatedName = GenerateRandomName();
            newStudentObj.name = generatedName;
            studentScript.studentName = generatedName;

            if (availablePersonalities != null && availablePersonalities.Length > 0)
            {
                studentScript.personalityData = GetRandomPersonalityByWeight();
            }

            TMP_Text textUI = newStudentObj.GetComponentInChildren<TMP_Text>();
            if (textUI != null) textUI.text = $"{generatedName}:\n0/100";
            
            // 3D Model Aspect Generation
            studentAspectManager.GenerateStudentRandomAppearance(newStudentObj, studentScript.personalityData.personalityType);
        }
    }

    private StudentPersonalitySO GetRandomPersonalityByWeight()
    {
        int totalWeight = 0;
        foreach (var p in availablePersonalities) 
        {
            totalWeight += p.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);

        int currentWeightSum = 0;
        foreach (var p in availablePersonalities)
        {
            currentWeightSum += p.spawnWeight;
            if (randomValue < currentWeightSum)
            {
                return p; 
            }
        }
        
        return availablePersonalities[0]; 
    }

    string GenerateRandomName()
    {
        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }

    public void NextRound(int newAmount)
    {
        numberOfStudents = newAmount;
        SpawnStudentsInSeats();
    }
}