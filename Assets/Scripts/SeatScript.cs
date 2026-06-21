using UnityEngine;

public class Seat : MonoBehaviour
{
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
