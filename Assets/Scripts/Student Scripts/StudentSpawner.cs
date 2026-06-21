using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StudentSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject studentPrefab;
    public Transform seatsContainer; // Arrastra aquí el objeto "--- ASIENTOS ---"
    public int numberOfStudents = 4; 

    [Header("Base de Datos")]
    public StudentPersonalitySO[] availablePersonalities; 
    public string[] firstNames = new string[] { "Ana", "Luis", "Sofía", "Carlos", "Elena", "Marta", "Diego" };
    public string[] lastNames = new string[] { "Pérez", "Gómez", "López", "Martínez", "Rodríguez", "Sánchez" };

    void Start()
    {
        SpawnStudentsInSeats();
    }

    public void SpawnStudentsInSeats()
    {
        // 1. Obtenemos todas las sillas disponibles
        Seat[] allSeats = seatsContainer.GetComponentsInChildren<Seat>();
        List<Seat> availableSeats = new List<Seat>(allSeats);

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
            // 3. Elegimos una silla al azar y la quitamos de la lista para no repetir
            int randomIndex = Random.Range(0, availableSeats.Count);
            Seat chosenSeat = availableSeats[randomIndex];
            availableSeats.RemoveAt(randomIndex);

            // 4. Instanciamos al alumno (la posición no importa, porque el asiento lo moverá)
            GameObject newStudentObj = Instantiate(studentPrefab, transform.position, Quaternion.identity);
            Student studentScript = newStudentObj.GetComponent<Student>();

            // 5. ¡LA MAGIA! Usamos tu método del asiento para "magnetizarlo" y setear datos
            chosenSeat.AssignStudent(studentScript);

            // 6. Configuramos datos
            string generatedName = GenerateRandomName();
            newStudentObj.name = generatedName;
            studentScript.studentName = generatedName;

            if (availablePersonalities != null && availablePersonalities.Length > 0)
            {
                studentScript.personalityData = availablePersonalities[Random.Range(0, availablePersonalities.Length)];
            }

            // Actualizamos UI
            TMP_Text textUI = newStudentObj.GetComponentInChildren<TMP_Text>();
            if (textUI != null) textUI.text = $"{generatedName}:\n0/100";
        }
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