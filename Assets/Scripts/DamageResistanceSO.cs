using System;
using UnityEngine;

namespace TD.Combat
{
    [CreateAssetMenu(menuName = "TD/Combat/Damage Resistance Profile")]
    public class DamageResistanceSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public DamageType type;
            [Range(0f, 5f)] public float multiplier; // 1.0 = normal, 0.5 = half dmg, 2.0 = double dmg
        }

        [Tooltip("Used if a type is not listed in Entries.")]
        [Range(0f, 5f)] public float defaultMultiplier = 1f;

        public Entry[] entries = Array.Empty<Entry>();

        public float GetMultiplier(DamageType type)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].type.Equals(type))
                        return Mathf.Max(0f, entries[i].multiplier);
                }
            }
            return Mathf.Max(0f, defaultMultiplier);
        }
    }
}
