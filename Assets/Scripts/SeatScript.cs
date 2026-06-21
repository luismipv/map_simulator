using UnityEngine;

public class Seat : MonoBehaviour
{
   [Header("Estado del Asiento")]
    public Student currentStudent;

    public void AssignStudent(Student newStudent)
    {
        currentStudent = newStudent;
        if(newStudent != null)
        {
            newStudent.currentSeat = this;
            newStudent.transform.position = this.transform.position; 
        }
    }
}
