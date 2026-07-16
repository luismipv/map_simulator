using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct MultiplierIconData
{
    public ModifierID modifierID;
    public Sprite icon;
}

public class MultiplierIcons : MonoBehaviour
{
    private static Dictionary<ModifierID, Sprite> multiplierIcons = new Dictionary<ModifierID, Sprite>();
    [SerializeField] public List<MultiplierIconData> multiplierIconData = new List<MultiplierIconData>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        foreach (var icon in multiplierIconData)
        {
            multiplierIcons.Add(icon.modifierID, icon.icon);
        }
    }

    public static Sprite GetIcon(ModifierID modifierID)
    {
        return multiplierIcons[modifierID];
    }
}
