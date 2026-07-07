using UnityEngine;

[CreateAssetMenu(fileName = "NuevoLayout", menuName = "MAP Simulator/Layout de Salón")]
public class LayoutData : ScriptableObject
{
    public string layoutName = "Salón Nuevo";
    [Tooltip("El Prefab armado por Diego con todas las sillas")]
    public GameObject layoutPrefab;
    
    [Header("Configuración de Cámara")]
    public Vector3 idealCameraPosition = new Vector3(0, 10, -10);
    public float idealCameraSize = 5f; 
}