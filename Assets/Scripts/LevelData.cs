using UnityEngine;

[CreateAssetMenu(fileName = "Nivel_", menuName = "MAP Simulator/Nivel de Juego")]
public class LevelData : ScriptableObject
{
    [Header("Economía y Reglas")]
    public int startingMoney = 0;
    public int moneyPerPass = 50;
    public int maxDropouts = 3;
    [Tooltip("Activa esto si quieres que los padres se quejen y te cobren multas")]
    public bool enableMoneyFines = false; // <-- Aquí está el Mutador de Multa

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