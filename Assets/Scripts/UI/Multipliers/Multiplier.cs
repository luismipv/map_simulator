using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MultiplierType { Learning, Stress }

[System.Serializable]
public class Multiplier : MonoBehaviour
{
    public TextMeshProUGUI multiplierText;
    private float _multiplierValue;
    public Image multiplierIcon;
    public ModifierID modifierID;

    [Header("Insignia / Badge")]
    public MultiplierType multiplierType = MultiplierType.Learning;
    public Image badgeIcon;
    public Sprite learningBadgeSprite;
    public Sprite stressBadgeSprite;
    
    private Coroutine expandCoroutine; // Para no multiplicar las animaciones

    private bool _isInitialized = false;

    // Set data with modifier ID and value
    public void SetData(ModifierID modifierID, float value)
    {
        SetData(modifierID, value, null, Color.white, MultiplierType.Learning);
    }

    // Overload for setting data with icon and color
    public void SetData(ModifierID modifierID, float value, Sprite icon, Color color)
    {
        SetData(modifierID, value, icon, color, MultiplierType.Learning);
    }

    // Overload for setting data with icon, color and multiplier type
    public void SetData(ModifierID modifierID, float value, Sprite icon, Color color, MultiplierType type, float delay = 0f, float pitchMultiplier = 1f)
    {
        this.multiplierType = type;
        float previousValue = _multiplierValue;
        bool wasInitialized = _isInitialized;

        this.modifierID = modifierID;
        multiplierText.text = $"x{value:F1}";
        _multiplierValue = value;
        _isInitialized = true;

        if (Mathf.Approximately(value, 1f))
        {
            this.gameObject.SetActive(false);
            return;
        }

        bool wasActiveInHierarchy = this.gameObject.activeInHierarchy;
        this.gameObject.SetActive(true);
        
        if (icon != null && multiplierIcon != null)
        {
            multiplierIcon.sprite = icon;
        }
        if (multiplierIcon != null) multiplierIcon.color = color;
        if (multiplierText != null) multiplierText.color = color;

        // Cambiar sprite y color del badge según sea Aprendizaje o Estrés
        if (badgeIcon != null)
        {
            Sprite targetBadge = (type == MultiplierType.Learning) ? learningBadgeSprite : stressBadgeSprite;
            if (targetBadge != null)
            {
                badgeIcon.sprite = targetBadge;
                badgeIcon.enabled = true;
                badgeIcon.color = color;
                badgeIcon.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[Multiplier] Falta asignar el sprite de {type} (learningBadgeSprite/stressBadgeSprite) en el Prefab de Multiplier.");
            }
        }

        // Evitar doble animación/sonido: animar solo al nacer o al cruzar el umbral de 1f
        bool thresholdCrossed = wasInitialized && ((previousValue <= 1f && value > 1f) || (previousValue >= 1f && value < 1f));
        bool isNewActivation = !wasActiveInHierarchy || !wasInitialized;

        if (isNewActivation || thresholdCrossed)
        {
            TriggerExpandAnimation(delay, pitchMultiplier);
        }
    }

    void OnEnable()
    {
        // Solo disparar animación en OnEnable si ya estaba inicializado de antes
        if (_isInitialized && !Mathf.Approximately(_multiplierValue, 1f))
        {
            TriggerExpandAnimation(0f, 1f);
        }
    }

    private void TriggerExpandAnimation(float delay = 0f, float pitchMultiplier = 1f)
    {
        if (expandCoroutine != null) StopCoroutine(expandCoroutine);
        expandCoroutine = StartCoroutine(ExpandFromZero(0.3f, delay, pitchMultiplier));
    }

    // Coroutine to expand this object's scale from 0 to 1 smoothly with optional stagger delay and pitch
    [SerializeField]
    private AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField]
    private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    public System.Collections.IEnumerator ExpandFromZero(float duration = 0.3f, float delay = 0f, float pitchMultiplier = 1f)
    {
        if (delay > 0f)
        {
            transform.localScale = Vector3.zero;
            yield return new WaitForSeconds(delay);
        }

        transform.localScale = Vector3.one * 2f;
        float initialRotation = Random.Range(-30f, 30f); 
        transform.localRotation = Quaternion.Euler(0f, 0f, initialRotation);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            
            float curveVal = expandCurve.Evaluate(t);
            float scale = Mathf.LerpUnclamped(2f, 1f, curveVal);

            float rCurveVal = rotationCurve.Evaluate(t);
            float rotation = Mathf.LerpUnclamped(initialRotation, 0f, rCurveVal);

            transform.localScale = new Vector3(scale, scale, scale);
            transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_multiplierValue != 1)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PostEvent("UI_Stamp_Play", this.gameObject, pitchMultiplier);
        }
        
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}