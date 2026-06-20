using UnityEngine;
using TMPro; // Necesario para modificar TextMeshPro

public class StudentSpawner : MonoBehaviour
{
    [Header("Configuración del Spawner")]
    public GameObject studentPrefab;
    public float spacingX = 5f; // Separé X e Y por si tu prefab es más ancho que alto
    public float spacingY = 3f; 

    private int columns = 4;
    private int rows = 2;

    [Header("Generador de Nombres")]
    public string[] firstNames = new string[5] { "Ana", "Luis", "Sofía", "Carlos", "Elena" };
    public string[] lastNames = new string[5] { "Pérez", "Gómez", "López", "Martínez", "Rodríguez" };

    void Start()
    {
        SpawnSymmetricGrid();
    }

    void SpawnSymmetricGrid()
    {
        // Ahora calculamos para X e Y (no Z)
        float startX = -(columns - 1) * spacingX / 2f;
        float startY = -(rows - 1) * spacingY / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Posicionamos en X e Y. La Z se queda en 0.
                Vector3 spawnPosition = transform.position + new Vector3(
                    startX + (x * spacingX), 
                    startY + (y * spacingY), 
                    0f 
                );

                GameObject newStudent = Instantiate(studentPrefab, spawnPosition, Quaternion.identity);

                string generatedName = GenerateRandomName();
                newStudent.name = generatedName;
                newStudent.GetComponent<Student>().studentName = generatedName; 
               // Debug.Log(newStudent.GetComponent<Student>().studentName); // Verificamos que el nombre se asignó correctamente
                // Asignamos el nombre al script del estudiante
                 // Cambia el nombre en la jerarquía

                // --- NUEVO: Cambiar el texto en la pantalla ---
                // Busca el componente de texto dentro del prefab instanciado
                TMP_Text textUI = newStudent.GetComponentInChildren<TMP_Text>();
                
                if (textUI != null)
                {
                    // Le ponemos el nombre generado y le concatenamos el puntaje que tenías
                    textUI.text = $"{generatedName}:\n0/100";
                }
                else
                {
                    Debug.LogWarning("No se encontró un componente TextMeshPro en el Prefab del estudiante.");
                }

                newStudent.transform.SetParent(this.transform);
            }
        }
    }

    string GenerateRandomName()
    {
        int nameIndex = Random.Range(0, firstNames.Length);
        int lastNameIndex = Random.Range(0, lastNames.Length);
        return $"{firstNames[nameIndex]} {lastNames[lastNameIndex]}";
    }
}