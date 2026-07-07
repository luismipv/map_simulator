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