// File: Combat.cs
using System.Reflection;
using UnityEngine;

/// <summary>
/// Central damage entry point used by projectiles and hitscan.
/// Multiplies incoming damage by target Stats (DamageTaken) before applying to Health.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Apply a hit to the collider's owning Health, after multiplying by any DamageTaken stat.
    /// Expects a DamageInfo struct defined elsewhere in your project.
    /// </summary>
    public static void ApplyHit(Collider target, DamageInfo info)
    {
        if (!target) return;

        // Find the impacted GameObject and Health
        var go = target.attachedRigidbody ? target.attachedRigidbody.gameObject : target.gameObject;
        var health = go ? go.GetComponentInParent<Health>() : null;
        if (!health) return;

        // Base damage (amount * crit)
        float dmg = Mathf.Max(0f, info.amount) * Mathf.Max(0f, (info.critMult <= 0f ? 1f : info.critMult));

        // Apply DamageTaken multiplier from StatsComponent if present (default 1.0)
        var stats = health.GetComponentInParent<StatsComponent>();
        if (stats)
        {
            float dmgTakenMul = Mathf.Max(0f, stats.GetFinal(StatIDs.DamageTaken, 1f));
            dmg *= dmgTakenMul;
        }

        // If your Health class has resist handling internally, prefer calling a method on it.
        if (TryInvokeHealthDamage(health, dmg, info)) return;

        // Fallback: direct HP write (minimal; assumes Health.currentHP is public like in your project)
        health.currentHP -= dmg;

        // Handle death if necessary
        if (health.currentHP <= 0f)
        {
            health.currentHP = 0f;

            // Try to invoke common death handlers first
            if (TryInvoke(health, "Die") || TryInvoke(health, "Kill") || TryInvoke(health, "OnDeathInternal"))
                return;

            // As a last resort, recycle enemy or destroy the GO to prevent stuck UI/objects
            var agent = health.GetComponentInParent<EnemyAgentFlow>();
            if (agent && EnemyPool.Instance)
            {
                EnemyPool.Instance.Recycle(agent);
            }
            else
            {
                Object.Destroy(health.gameObject);
            }
        }
    }

    // --- helpers ---

    /// <summary>
    /// Try common Health damage APIs via reflection to stay compatible with your current Health implementation.
    /// Supported signatures (in order tried):
    ///   void ApplyDamage(float amount, DamageType type, GameObject source, Vector3 hitPoint)
    ///   void TakeDamage (float amount, DamageType type, GameObject source, Vector3 hitPoint)
    ///   void Damage     (float amount, DamageType type, GameObject source, Vector3 hitPoint)
    ///   void ReceiveDamage(float amount, DamageType type, GameObject source, Vector3 hitPoint)
    ///   (also variants without source/point, or just (float amount))
    /// Returns true if a method was invoked.
    /// </summary>
    private static bool TryInvokeHealthDamage(Health h, float dmg, DamageInfo info)
    {
        var t = h.GetType();

        // Candidate method names in preferred order
        string[] names = { "ApplyDamage", "TakeDamage", "Damage", "ReceiveDamage" };

        // Candidate parameter sets (loosely matched)
        object[][] paramSets = new object[][]
        {
            new object[] { dmg, info.type, info.source, info.hitPoint },
            new object[] { dmg, info.type, info.hitPoint, info.source },
            new object[] { dmg, info.type, info.source },
            new object[] { dmg, info.type },
            new object[] { dmg }
        };

        foreach (var name in names)
        {
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                if (m.Name != name) continue;
                var pars = m.GetParameters();

                foreach (var set in paramSets)
                {
                    if (pars.Length != set.Length) continue;
                    if (!ParametersMatch(pars, set)) continue;

                    try
                    {
                        m.Invoke(h, set);
                        return true;
                    }
                    catch { /* keep trying */ }
                }
            }
        }

        return false;
    }

    private static bool ParametersMatch(ParameterInfo[] pars, object[] args)
    {
        for (int i = 0; i < pars.Length; i++)
        {
            var pt = pars[i].ParameterType;
            var a = args[i];

            if (a is float && pt != typeof(float)) return false;
            if (a is DamageType && pt != typeof(DamageType)) return false;
            if (a is GameObject && pt != typeof(GameObject)) return false;
            if (a is Vector3 && pt != typeof(Vector3)) return false;
        }
        return true;
    }

    private static bool TryInvoke(object obj, string method)
    {
        var m = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) return false;
        try { m.Invoke(obj, null); return true; } catch { return false; }
    }
}
