using UnityEngine;

[CreateAssetMenu(fileName = "New Synergy Rule", menuName = "Sinergias/Regla")]
public class SynergyRuleSO : ScriptableObject
{
    public StudentPersonality personalityA;
    public StudentPersonality personalityB;

    [Header("Efectos para Alumno A")]
    public float learningMultA = 1f;
    public float stressMultA = 1f;

    [Header("Efectos para Alumno B")]
    public float learningMultB = 1f;
    public float stressMultB = 1f;

    public bool Matches(StudentPersonality type1, StudentPersonality type2)
    {
        return (personalityA == type1 && personalityB == type2) || 
               (personalityA == type2 && personalityB == type1);
    }
}