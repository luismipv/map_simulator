using UnityEngine;
using System.Collections.Generic;

public class Seat : MonoBehaviour
{
    public static List<Seat> AllSeats = new List<Seat>();

    private void OnEnable()
    {
        if (!AllSeats.Contains(this))
            AllSeats.Add(this);
    }

    private void OnDisable()
    {
        AllSeats.Remove(this);
    }

    [Header("Estado del Asiento")]
    public Student currentStudent;

    public void AssignStudent(Student newStudent)
    {
        currentStudent = newStudent;
        
        if (newStudent != null)
        {
            newStudent.currentSeat = this; 
            newStudent.transform.SetParent(this.transform); 
            
            // Lo regresamos a 0. ¡Con el colisionador de la silla eliminado, ya no estorba!
            newStudent.transform.localPosition = Vector3.zero; 
        }
    }
}
