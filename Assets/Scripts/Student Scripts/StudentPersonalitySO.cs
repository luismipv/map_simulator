using UnityEngine;
using System.Collections.Generic; // Ojo: Necesitamos esto para usar Lists

// 1. ESTO NO ES UN SCRIPTABLE OBJECT. Es solo una "cajita" de datos 
// que le dice a Unity cómo agrupar las variables en el Inspector.
[System.Serializable]
public struct ToolReaction
{
    // Usamos tu clase base abstracta
    public TeacherTool tool; 
    
    public float stressMod;
    public float learningMod;
    public float successChance;
}

[System.Serializable]
public struct GlobalToolReaction
{
    public GlobalTool globalTool;
    public float stressMod;
    public float learningMod;
    public float successChance;
}

[CreateAssetMenu(fileName = "StudentPersonalitySO", menuName = "Scriptable Objects/StudentPersonalitySO")]
public class StudentPersonalitySO : ScriptableObject
{
    public StudentPersonality personalityType;
    public string personalityNameEs; 

    [Header("Multiplicadores Generales")]
    public float learningRateMod = 1f;
    public float stressRateMod = 1f;
    public float recoveryRateMod = 1f;

    [Header("Aparición")]
    public int spawnWeight = 10; // 10 será el valor por defecto

    // 2. BORRAMOS LAS VARIABLES SUELTAS Y PONEMOS ESTO:
    // Una lista dinámica donde puedes agregar tantas herramientas como quieras
    [Header("Reacciones Dinámicas a Herramientas")]
    public List<ToolReaction> toolReactions = new List<ToolReaction>();
    public List<GlobalToolReaction> globalToolReactions = new List<GlobalToolReaction>();

    // 3. Función inteligente para que la herramienta sepa cómo afecta a este alumno
    public ToolReaction GetReactionForTool(TeacherTool toolToCheck)
    {
        foreach (ToolReaction reaction in toolReactions)
        {
            // Si la herramienta que usamos está en la lista de este alumno, devolvemos sus mods
            if (reaction.tool == toolToCheck)
            {
                return reaction; 
            }
        }

        // Si no la pusiste en la lista, devolvemos una reacción neutra (x1)
        return new ToolReaction { tool = toolToCheck, stressMod = 1f, learningMod = 1f , successChance = 100f};
    }

    public GlobalToolReaction GetReactionForGlobalTool(GlobalTool globalToolToCheck)
    {
        foreach (GlobalToolReaction reaction in globalToolReactions)
        {
            if(reaction.globalTool == globalToolToCheck)
            {
                return reaction;
            }
        }
        return new GlobalToolReaction { globalTool = globalToolToCheck, stressMod = 1f, learningMod = 1f , successChance = 100f};
    }
}