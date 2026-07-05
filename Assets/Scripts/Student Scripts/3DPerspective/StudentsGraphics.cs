using UnityEngine;

[System.Serializable]
public struct PersonalityMeshEntry
{
    public StudentPersonality personality;
    public Mesh[] bodyMeshes;
}

[System.Serializable]
public struct StudentAppearance
{
    public Mesh bodyMesh;
    public Mesh hairMesh;
}

[CreateAssetMenu(fileName = "StudentsGraphics", menuName = "Scriptable Objects/StudentsGraphics")]
public class StudentsGraphics : ScriptableObject
{
    [Header("Meshes por Personalidad")]
    public PersonalityMeshEntry[] personalityMeshes;

    [Header("Estilos de Cabello")]
    public Mesh[] hairMeshes;

    public StudentAppearance GetRandomAppearance(StudentPersonality personality)
    {
        Mesh bodyMesh = PickRandomMesh(FindBodyMeshes(personality));
        Mesh hairMesh = PickRandomMesh(hairMeshes);

        return new StudentAppearance
        {
            bodyMesh = bodyMesh,
            hairMesh = hairMesh
        };
    }

    private Mesh[] FindBodyMeshes(StudentPersonality personality)
    {
        if (personalityMeshes == null) return null;

        foreach (PersonalityMeshEntry entry in personalityMeshes)
        {
            if (entry.personality == personality)
                return entry.bodyMeshes;
        }

        return null;
    }

    private static Mesh PickRandomMesh(Mesh[] meshes)
    {
        if (meshes == null || meshes.Length == 0) return null;

        return meshes[Random.Range(0, meshes.Length)];
    }
}
