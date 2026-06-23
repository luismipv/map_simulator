using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("Configuración de Animación")]
    public float moveSpeed = 2f; // Ajusta esto dependiendo de la escala de tu Canvas
    public float duration = 1.5f; 
    
    private TextMeshProUGUI textTmp;

    public void Setup(string text, Color color)
    {
        textTmp = GetComponent<TextMeshProUGUI>();
        textTmp.text = text;
        textTmp.color = color;
        
        StartCoroutine(FloatingRoutine());
    }

    private IEnumerator FloatingRoutine()
    {
        float timer = 0f;
        Color originalColor = textTmp.color;
        
        while (timer < duration)
        {
            // 1. Sube lentamente
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // 2. Se desvanece (Alpha de 1 a 0)
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);
            textTmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Se destruye para limpiar memoria
        Destroy(gameObject);
    }
}