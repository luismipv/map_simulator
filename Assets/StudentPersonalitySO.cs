using UnityEngine;

[CreateAssetMenu(fileName = "StudentPersonalitySO", menuName = "Scriptable Objects/StudentPersonalitySO")]
public class StudentPersonalitySO : ScriptableObject
{
    public StudentPersonality personalityType;
    public string personalityNameEs; // Para mostrar en la UI (ej: "Ansioso")

    [Header("Multiplicadores de Aprendizaje")]
    public float learningRateMod = 1f;
    
    [Header("Multiplicadores de Estrés")]
    public float stressRateMod = 1f;
    public float recoveryRateMod = 1f;
    
}
