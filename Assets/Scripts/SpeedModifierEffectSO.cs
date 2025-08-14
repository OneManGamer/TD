// File: SpeedModifierEffectSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Status/Speed Modifier")]
public class SpeedModifierEffectSO : StatusEffectSO
{
    [Tooltip("Multiply enemy MoveSpeed by this factor while active. 1 = no change; 0.5 = 50% slow.")]
    [Range(0f, 2f)] public float speedMultiplier = 0.7f;

    [Tooltip("Order within the modifier stack (lower applies earlier). 200 is a good default for MUL.")]
    public int order = 200;

    public override IEnumerable<StatModifier> BuildModifiers(StatsComponent target, GameObject source)
    {
        yield return new StatModifier(StatIDs.MoveSpeed, StatOp.Mul, Mathf.Max(0f, speedMultiplier), order);
    }
}
