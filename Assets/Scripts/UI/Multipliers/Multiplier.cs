using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Multiplier : MonoBehaviour
{
    public TextMeshProUGUI multiplierText;
    private float _multiplierValue;
    public Image multiplierIcon;
    public ModifierID modifierID;

    // Set data with modifier ID and value
    public void SetData(ModifierID modifierID, float value)
    {
        this.modifierID = modifierID;
        multiplierText.text = $"x{value:F1}";
        _multiplierValue = value;
        if (value == 1f){
            this.gameObject.SetActive(false);
        }
    }

    // Overload for setting data with icon and color
    public void SetData(ModifierID modifierID, float value, Sprite icon, Color color){
        float previousValue = _multiplierValue;
        SetData(modifierID, value);
        if (icon != null)
        {
            multiplierIcon.sprite = icon;
        }
        multiplierIcon.color = color;
        multiplierText.color = color;

        if(previousValue < 1f && value > 1f || previousValue > 1f && value < 1f){
            StartCoroutine(ExpandFromZero(0.3f));
        }
    }

    void OnEnable(){
        StartCoroutine(ExpandFromZero(0.3f));
    }

    // Coroutine to expand this object's scale from 0 to 1 smoothly
    [SerializeField]
    private AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField]
    private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    public System.Collections.IEnumerator ExpandFromZero(float duration = 0.3f)
    {
        // Start at scale 2 and a random "stamp" rotation, tween to scale 1 and rot 0
        transform.localScale = Vector3.one * 2f;
        float initialRotation = Random.Range(-30f, 30f); // Random stamp angle
        transform.localRotation = Quaternion.Euler(0f, 0f, initialRotation);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Use the curve for scale (default: overshoot/elastic recommended curve in inspector)
            float curveVal = expandCurve.Evaluate(t);
            float scale = Mathf.LerpUnclamped(2f, 1f, curveVal);

            // Optionally, use a separate curve for rotation easing if desired
            float rCurveVal = rotationCurve.Evaluate(t);
            float rotation = Mathf.LerpUnclamped(initialRotation, 0f, rCurveVal);

            transform.localScale = new Vector3(scale, scale, scale);
            transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure exactly 1 and 0° at end
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
