// File: StatModifier.cs
using UnityEngine;

[System.Serializable]
public class StatModifier
{
    public string statId;
    public StatOp op = StatOp.Add;
    public float value = 0f;

    /// <summary>Lower order applies earlier. You can layer adds, then muls, then overrides.</summary>
    public int order = 0;

    /// <summary>Source token for grouping/removal (set by StatusRuntime when applying effects).</summary>
    [System.NonSerialized] public object sourceToken;

    public StatModifier() { }

    public StatModifier(string statId, StatOp op, float value, int order = 0, object sourceToken = null)
    {
        this.statId = statId;
        this.op = op;
        this.value = value;
        this.order = order;
        this.sourceToken = sourceToken;
    }
}
