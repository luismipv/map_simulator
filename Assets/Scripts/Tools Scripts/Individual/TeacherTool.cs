using UnityEngine;

public abstract class TeacherTool : ScriptableObject
{
    public string toolName;
    [TextArea(2, 5)]public string toolDescription;
     public Sprite toolIcon;

    public abstract void ApplyToolEffect(Student student, Logic gamelogic); // Método abstracto que cada herramienta implementará para aplicar su efecto específico
    
}
