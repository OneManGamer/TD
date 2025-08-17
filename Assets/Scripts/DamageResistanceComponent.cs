using System;
using UnityEngine;

namespace TD.Combat
{
    /// <summary>
    /// Attach to an enemy to provide damage-type multipliers at runtime.
    /// Uses per-instance overrides first, then falls back to a shared DamageResistanceSO profile.
    /// </summary>
    public class DamageResistanceComponent : MonoBehaviour
    {
        [Header("Per-instance overrides (optional)")]
        public ResistanceOverride[] overridesArray;

        [Header("Shared profile (optional)")]
        public DamageResistanceSO profile;

        [Header("Global tweak")]
        [Tooltip("Applied on top of whatever multiplier is found (useful for quick tuning).")]
        [Range(0f, 5f)] public float globalMultiplier = 1f;

        [Serializable]
        public struct ResistanceOverride
        {
            public DamageType type;
            [Tooltip("0.5 = half damage; 1.0 = normal; 2.0 = double damage")]
            public float multiplier;
        }

        /// <summary>
        /// Main API used by CombatIntegrationAPI.
        /// </summary>
        public float GetMultiplier(DamageType type)
        {
            // 1) Per-instance override wins
            if (overridesArray != null)
            {
                for (int i = 0; i < overridesArray.Length; i++)
                {
                    if (overridesArray[i].type.Equals(type))
                        return Mathf.Max(0f, overridesArray[i].multiplier) * globalMultiplier;
                }
            }

            // 2) Fall back to profile if present
            if (profile != null)
            {
                float m = profile.GetMultiplier(type);
                return Mathf.Max(0f, m) * globalMultiplier;
            }

            // 3) Default (no resistance specified)
            return 1f * globalMultiplier;
        }
    }
}
