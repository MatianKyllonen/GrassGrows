using UnityEngine;

public abstract class Upgrade : ScriptableObject
{
    public string title = "Upgrade";
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("How many times this card can be picked")]
    public int maxStacks = 5;

    // stack = how many times it has been picked, including this one (1, 2, 3...)
    public abstract void Apply(PlayerController player, int stack);
}