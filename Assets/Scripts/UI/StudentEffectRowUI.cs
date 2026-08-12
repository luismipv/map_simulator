using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudentEffectRowUI : MonoBehaviour
{
    [Header("Componentes UI")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI badgeText;
    public TextMeshProUGUI flavorPhraseText;
    public Image cardBackground;
    public Image badgeBackground;

    [Header("Colores")]
    public Color positiveColor = new Color(0.3098f, 0.6980f, 0.5254f, 1f);
    public Color negativeColor = new Color(0.9490f, 0.4274f, 0.4941f, 1f);
    public Color positiveBgColor = new Color(0.3098f, 0.6980f, 0.5254f, 0.15f);
    public Color negativeBgColor = new Color(0.9490f, 0.4274f, 0.4941f, 0.15f);

    public void SetupEffect(string title, string badge, string flavorPhrase, bool isPositive, Sprite icon = null)
    {
        if (titleText != null) titleText.text = title;
        if (badgeText != null) badgeText.text = badge;
        if (flavorPhraseText != null) flavorPhraseText.text = flavorPhrase;

        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        Color mainColor = isPositive ? positiveColor : negativeColor;
        Color bgColor = isPositive ? positiveBgColor : negativeBgColor;

        if (badgeText != null) badgeText.color = mainColor;
        if (badgeBackground != null) badgeBackground.color = bgColor;
        if (cardBackground != null) cardBackground.color = bgColor;
    }
}
