using UnityEngine;
using System.Collections.Generic;

public enum SpawnMode { RandomWithWeights, FixedList }

[CreateAssetMenu(fileName = "Nivel_", menuName = "MAP Simulator/Nivel de Juego")]
public class LevelData : ScriptableObject
{
    [Header("Configuración del Salón")]
    [Tooltip("Arrastra aquí el ScriptableObject del Layout (Ej. Salón Pasillo)")]
    public LayoutData classroomLayout; 

    [Header("Alumnos a Generar")]
    public SpawnMode spawnMode = SpawnMode.RandomWithWeights;
    public int totalRandomStudents = 5; 
    public List<StudentPersonalitySO> fixedStudentRoster;

    // ==========================================
    // --- INTERRUPTORES DE MECÁNICAS ---
    // ==========================================
    [Header("Mecánicas Activas (Tutorial)")]
    public bool enableTimer = true;
    public bool enableDistractions = true;
    public bool enableSynergies = true;
    public bool enableSpatialEffects = true;

    // ==========================================
    // --- MODO TUTORIAL ---
    // ==========================================
    [Header("Modo Tutorial")]
    [Tooltip("Si está apagado, este será un Nivel Normal y no habrá textos de guía")]
    public bool isTutorialLevel = false;

    [Tooltip("Si es tutorial, arrastra aquí el siguiente nivel (Ej. Nivel_Tutorial_2). Si se deja vacío, terminará normalmente.")]
    public LevelData nextLevel;

    [Tooltip("Multiplicador de velocidad. 1 = Normal, 2 = Doble de rápido, 3 = Triple. ¡Ideal para que el tutorial sea ágil!")]
    [Range(1f, 6f)]
    public float learningSpeedMultiplier = 1f;

    // ==========================================
    // --- SISTEMA DINÁMICO DE TUTORIAL ---
    // ==========================================
    [System.Serializable]
    public class TutorialSequence
    {
        public string noteName; // Solo para que se vea ordenado en el Inspector
        public TutorialTrigger triggerType; // ¿Qué detona este tutorial?
        public List<TutorialStepSO> steps; // Las tarjetas a mostrar
        public bool triggerOnlyOnce = true; // ¿Solo sale la primera vez?
        [HideInInspector] public bool hasTriggered = false; // Candado interno
    }

    [Header("Eventos del Tutorial")]
    [Tooltip("Agrega aquí las secuencias de tutorial y qué acción las detona.")]
    public List<TutorialSequence> tutorialSequences = new List<TutorialSequence>();

    [Header("Mazo de Herramientas")]
    [Tooltip("Las herramientas que el maestro puede usar en este nivel")]
    public List<TeacherTool> allowedTools; 


    // --- ¡LA NUEVA LISTA! ---
    [Header("Mazo de Herramientas Globales")]
    [Tooltip("Las herramientas para toda la clase (Ej. Chiste, Examen Sorpresa)")]
    public List<GlobalTool> allowedGlobalTools;

    // ==========================================
    // --- REGLAS GENERALES ---
    // ==========================================
    [Header("Economía y Reglas")]
    public int startingMoney = 0;
    public int moneyPerPass = 50;
    public int maxDropouts = 3;
    public bool enableMoneyFines = false;

    [Header("Semestre")]
    public int totalPartials = 3;
    public float initialLearningQuota = 100f; 
    public float quotaIncreasePerPartial = 50f;

    [Header("Tiempos")]
    public float maxGlobalTimer = 120f; 
    public float minGlobalTimer = 70f;
    public float timeReductionPerPartial = 30f;
    public float maxEndSemesterMultiplier = 2f; 
}