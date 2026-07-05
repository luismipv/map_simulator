using UnityEngine;

public class StudentAspectManager : MonoBehaviour
{
    public StudentsGraphics studentsGraphics;

    public GameObject GenerateStudentRandomAppearance(GameObject studentPrefab, StudentPersonality studentPersonality)
    {
        Student3D student = studentPrefab.GetComponent<Student3D>();
        if(student == null)
        {
            Debug.LogError("Student prefab does not have a Student3D component");
            return studentPrefab;
        }
        StudentAppearance studentAppearance = studentsGraphics.GetRandomAppearance(studentPersonality);
        if(studentAppearance.bodyMesh != null){
            student.bodyMesh.sharedMesh = studentAppearance.bodyMesh;
        }
        if(studentAppearance.hairMesh != null){
            student.hairMesh.mesh = studentAppearance.hairMesh;
        }
        return studentPrefab;
    }
}
