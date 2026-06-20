using UnityEngine;
using TMPro;

public class StudentSpawner : MonoBehaviour
{
    [Header("Configuración Principal")]
    public GameObject studentPrefab;
    
    [Range(1, 20)] // Un deslizador en Unity para elegir rápido
    public int numberOfStudents = 4; 

    [Header("Márgenes de Pantalla")]
    public float marginX = 2f; // Espacio libre a los lados
    public float marginY = 3f; // Espacio libre arriba y abajo (para dejar lugar a tu UI)
    public float maxSpacingX = 4f; 
    public float maxSpacingY = 3f;

    [Header("Base de Datos de Personalidades")]
    public StudentPersonalitySO[] availablePersonalities; 

    [Header("Generador de Nombres")]
    public string[] firstNames = new string[] { "Ana", "Luis", "Sofía", "Carlos", "Elena" };
    public string[] lastNames = new string[] { "Pérez", "Gómez", "López", "Martínez", "Rodríguez" };

    void Start()
    {
        SpawnDynamicGrid();
    }

       void SpawnDynamicGrid()
    {
        // --- NUEVO: CALCULAMOS EL TAMAÑO REAL DE LA PANTALLA ---
        Camera cam = Camera.main;
        
        // orthographicSize es la mitad del alto de la pantalla. Lo multiplicamos por 2.
        float screenHeight = cam.orthographicSize * 2f;
        // El ancho es el alto multiplicado por la relación de aspecto (ej. 16:9)
        float screenWidth = screenHeight * cam.aspect;

        // Nuestra área de trabajo es la pantalla menos los márgenes que elijas
        float dynamicMaxWidth = screenWidth - marginX;
        float dynamicMaxHeight = screenHeight - marginY;

        // 1. Calculamos columnas y filas
        int columns = Mathf.CeilToInt(Mathf.Sqrt(numberOfStudents));
        int rows = Mathf.CeilToInt((float)numberOfStudents / columns);

        // 2. Calculamos el espaciado usando nuestros límites dinámicos
        float currentSpacingX = Mathf.Min(dynamicMaxWidth / columns, maxSpacingX);
        float currentSpacingY = Mathf.Min(dynamicMaxHeight / rows, maxSpacingY);

        // 3. Sistema de Auto-Escala
        int maxDimension = Mathf.Max(columns, rows);
        float scaleFactor = Mathf.Clamp(2.5f / maxDimension, 0.4f, 1f);

        // 4. Calculamos el punto de inicio
        float startX = -(columns - 1) * currentSpacingX / 2f;
        float startY = (rows - 1) * currentSpacingY / 2f; 

        int studentsSpawned = 0;

        // ... (A partir de aquí el ciclo for se queda exactamente igual)
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (studentsSpawned >= numberOfStudents) return; 

                Vector3 spawnPosition = transform.position + new Vector3(
                    startX + (x * currentSpacingX),
                    startY - (y * currentSpacingY), 
                    0f
                );

                GameObject newStudent = Instantiate(studentPrefab, spawnPosition, Quaternion.identity);
                newStudent.transform.SetParent(this.transform);

                // Escala
                newStudent.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

                // Configuramos los datos
                Student studentScript = newStudent.GetComponent<Student>();
                string generatedName = GenerateRandomName();
                newStudent.name = generatedName;
                studentScript.studentName = generatedName;

                if (availablePersonalities != null && availablePersonalities.Length > 0)
                {
                    int randIndex = Random.Range(0, availablePersonalities.Length);
                    studentScript.personalityData = availablePersonalities[randIndex];
                }

                TMP_Text textUI = newStudent.GetComponentInChildren<TMP_Text>();
                if (textUI != null) textUI.text = $"{generatedName}:\n0/100";

                studentsSpawned++;
            }
        }
    }

    string GenerateRandomName()
    {
        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }

        public void NextRound(int newAmount)
    {
        // 1. Eliminamos a todos los alumnos actuales del salón
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 2. Actualizamos el número de estudiantes y redibujamos la cuadrícula responsiva
        numberOfStudents = newAmount;
        SpawnDynamicGrid();
    }
}