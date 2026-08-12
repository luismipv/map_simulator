using UnityEngine;

public class AutoGrid : MonoBehaviour
{
    [Header("Configuración del Grid")]
    public int filas = 3;      // ¡Nuevo! Límite de filas
    public int columnas = 3;   // Límite de columnas
    public float espacioX = 200f; 
    public float espacioY = 200f; 

    [ContextMenu("Alinear Asientos Ahora")]
    public void AlinearGrid()
    {
        int totalHijos = transform.childCount;
        if (totalHijos == 0) return;

        // Te avisa en consola si metiste más sillas de las que caben
        if (totalHijos > filas * columnas)
        {
            Debug.LogWarning("¡Ojo! Hay más asientos en la carpeta que espacios en tu Grid (" + (filas * columnas) + ").");
        }

        // 1. Calculamos cuántas filas REALMENTE estamos usando
        int filasReales = Mathf.Min(filas, Mathf.CeilToInt((float)totalHijos / columnas));
        
        // 2. Calculamos el centro vertical de todo el bloque
        float offsetY = ((filasReales - 1) * espacioY) / 2f;

        for (int i = 0; i < totalHijos; i++)
        {
            // Si ya llenamos el grid, ignoramos los objetos sobrantes
            if (i >= filas * columnas) break;

            int filaActual = i / columnas;
            int columnaActual = i % columnas;

            // 3. ¡LA MAGIA SIMÉTRICA! 
            // Contamos cuántos asientos hay específicamente en ESTA fila para centrarla
            int asientosEnEstaFila = Mathf.Min(columnas, totalHijos - (filaActual * columnas));
            
            // Calculamos el centro horizontal exclusivo de esta fila
            float offsetX = ((asientosEnEstaFila - 1) * espacioX) / 2f;

            Transform asiento = transform.GetChild(i);

            // 4. Aplicamos las posiciones
            float posX = (columnaActual * espacioX) - offsetX;
            float posY = -(filaActual * espacioY) + offsetY; 

            asiento.localPosition = new Vector3(posX, posY, 0f);
        }
    }
}