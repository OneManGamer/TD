// File: EnemySetup.cs
using UnityEngine;
using System.Reflection;

[DisallowMultipleComponent]
public class EnemySetup : MonoBehaviour
{
    [Header("Definition")]
    public EnemyDefinitionSO definition;

    [Header("Overrides")]
    [Tooltip("Extra multiplier applied when setting the mover's speed from the definition.")]
    public float moveSpeedMultiplier = 1f;

    void Awake()
    {
        if (!definition)
        {
            Debug.LogWarning($"{name}: EnemySetup has no definition assigned.");
            return;
        }

        // 1) Apply to Health (maxHP/currentHP + resistances)
        var h = GetComponent<Health>();
        if (h) h.InitFrom(definition, fillToMax: true);

        // 2) Apply to movement agent (try common field/prop names)
        //    Works for EnemyFlowAgent or similar without adding a compile-time dependency.
        var mover = GetComponent<MonoBehaviour>(); // placeholder to get 'this' GO; we'll scan all behaviours below
        var behaviours = GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            // Try "moveSpeed" then "speed"
            if (TrySetFloatMember(b, "moveSpeed", definition.moveSpeed * Mathf.Max(0.01f, moveSpeedMultiplier))) break;
            if (TrySetFloatMember(b, "speed",     definition.moveSpeed * Mathf.Max(0.01f, moveSpeedMultiplier))) break;
        }

        // 3) Apply to bounty component (if present)
        var bounty = GetComponent<BountyOnDeath>();
        if (bounty) bounty.definition = definition;
    }

    bool TrySetFloatMember(object obj, string memberName, float value)
    {
        if (obj == null) return false;
        var t = obj.GetType();

        // Property first
        var prop = t.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(float))
        {
            prop.SetValue(obj, value);
            return true;
        }

        // Then field
        var field = t.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(float))
        {
            field.SetValue(obj, value);
            return true;
        }

        return false;
    }
}
