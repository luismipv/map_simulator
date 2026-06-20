using UnityEngine;
using System;

public abstract class GlobalTool : ScriptableObject
{
    public string globalToolName;
    [TextArea(2, 5)]public string globalToolDescription;
     public Sprite globalToolIcon;

    public abstract void ApplyGlobalToolEffect(Logic gameLogic, Action onFinished); 
}
