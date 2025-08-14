// File: TintStatusEffectSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Status/Tint Only")]
public class TintStatusEffectSO : StatusEffectSO
{
    [Header("Tint")]
    public Color tintColor = new Color(0.2f, 0.7f, 1f, 1f); // default: blue-ish
    [Range(0f, 1f)] public float weight = 0.6f;
    public bool pulse = false;
    public float pulseSpeed = 4f;

    [Header("Channel")]
    [Tooltip("All tints with the same channel share one slot and refresh instead of stacking.")]
    public string tintChannel = "Tint";

    public override string GetStackingKey()
    {
        // All tint effects with the same channel share a single key -> refresh, no stacking
        return $"TINT:{tintChannel}";
    }

    public override IEnumerable<StatModifier> BuildModifiers(StatsComponent target, GameObject source)
    {
        // No stat change; visuals only.
        yield break;
    }

    public override void OnApplied(GameObject host, object token)
    {
        if (!host) return;
        var ta = host.GetComponent<TintAttachment>();
        if (!ta) ta = host.AddComponent<TintAttachment>();
        ta.AddTint(token, tintColor, weight, pulse, pulseSpeed);
    }

    public override void OnRemoved(GameObject host, object token)
    {
        if (!host) return;
        var ta = host.GetComponent<TintAttachment>();
        if (ta) ta.RemoveTint(token);
    }
}
