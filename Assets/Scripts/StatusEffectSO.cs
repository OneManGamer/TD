// File: StatusEffectSO.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for timed status effects. Effects contribute StatModifiers and may drive visuals.
/// Stacking policy: effects with the same Stacking Key will NOT stack; a new application refreshes duration.
/// </summary>
public abstract class StatusEffectSO : ScriptableObject
{
    [Tooltip("Duration in seconds. Set <= 0 for instant/one-shot effects.")]
    public float duration = 2f;

    [Header("Stacking")]
    [Tooltip("Effects with the same key refresh instead of stack. Leave empty to default to the effect class name.")]
    public string stackingKey = "";

    /// <summary>
    /// The key used to dedupe/refresh. Default: class name, or override via 'stackingKey'.
    /// </summary>
    public virtual string GetStackingKey()
    {
        return string.IsNullOrWhiteSpace(stackingKey) ? GetType().Name : stackingKey;
    }

    /// <summary>Create the modifiers this effect contributes for this target (optional).</summary>
    public virtual IEnumerable<StatModifier> BuildModifiers(StatsComponent target, GameObject source) { yield break; }

    /// <summary>Called right after modifiers are applied. 'host' is the GameObject with StatusRuntime.</summary>
    public virtual void OnApplied(GameObject host, object token) { }

    /// <summary>Called when the effect expires or is removed. Use 'token' to clear per-application visuals.</summary>
    public virtual void OnRemoved(GameObject host, object token) { }
}
