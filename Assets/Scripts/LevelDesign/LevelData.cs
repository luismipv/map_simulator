using UnityEngine;
using System.Collections.Generic;

public enum SpawnMode { RandomWithWeights, FixedList }

[CreateAssetMenu(fileName = "Nivel_", menuName = "MAP Simulator/Nivel de Juego")]
public class LevelData : ScriptableObject
{
    [Header("=== 1. IDENTIDAD DEL NIVEL ===")]
    [Tooltip("Arrastra aquí el ScriptableObject del Layout (Ej. Salón Pasillo)")]
    public LayoutData classroomLayout; 
    public bool isTutorialLevel = false;
    [Tooltip("Si es tutorial, arrastra aquí el siguiente nivel (Ej. Nivel_Tutorial_2).")]
    public LevelData nextLevel;

    [Space(15)]
    [Header("=== 2. ALUMNOS Y GENERACIÓN ===")]
    public SpawnMode spawnMode = SpawnMode.RandomWithWeights;
    public int totalRandomStudents = 5; 
    public List<StudentPersonalitySO> fixedStudentRoster;
    [Tooltip("Si este nivel es un tutorial, arrastra aquí tu Prefab del TutorialStudent.")]
    public GameObject tutorialStudentPrefabOverride;

    [Space(15)]
    [Header("=== 3. ECONOMÍA Y DIFICULTAD ===")]
    public int startingMoney = 0;
    public int moneyPerPass = 50;
    public int maxDropouts = 3;
    public bool enableMoneyFines = false;

    [Space(15)]
    [Header("=== 4. PROGRESIÓN Y TIEMPOS ===")]
    public int totalPartials = 3;
    public float initialLearningQuota = 100f; 
    public float quotaIncreasePerPartial = 50f;
    public float minGlobalTimer = 70f;
    public float maxGlobalTimer = 120f; 
    public float timeReductionPerPartial = 30f;
    public float maxEndSemesterMultiplier = 2f; 
    [Tooltip("Multiplicador de velocidad. 1 = Normal, 2 = Doble, etc.")]
    [Range(1f, 6f)] public float learningSpeedMultiplier = 1f;

    [Space(15)]
    [Header("=== 5. INTERRUPTORES DE MECÁNICAS ===")]
    public bool enableTimer = true;
    public bool enableDistractions = true;
    public bool enableSynergies = true;
    public bool enableSpatialEffects = true;

    [Space(15)]
    [Header("=== 6. MAZO DEL JUGADOR (LISTAS) ===")]
    [Tooltip("Las herramientas que el maestro puede usar en este nivel")]
    public List<TeacherTool> allowedTools; 
    [Tooltip("Las herramientas para toda la clase (Ej. Chiste, Examen Sorpresa)")]
    public List<GlobalTool> allowedGlobalTools;

    // ==========================================
    // --- SISTEMA DINÁMICO DE TUTORIAL ---
    // ==========================================
    [System.Serializable]
    public class TutorialSequence
    {
        public string noteName; 
        public TutorialTrigger triggerType; 
        public List<TutorialStepSO> steps; 
        public bool triggerOnlyOnce = true; 
        [HideInInspector] public bool hasTriggered = false; 
    }

    [Space(15)]
    [Header("=== 7. EVENTOS DEL TUTORIAL (LISTAS) ===")]
    [Tooltip("Agrega aquí las secuencias de tutorial y qué acción las detona.")]
    public List<TutorialSequence> tutorialSequences = new List<TutorialSequence>();
}