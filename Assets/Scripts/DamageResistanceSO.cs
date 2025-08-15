using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  [CreateAssetMenu(menuName = "TD/Damage Resistance Profile", fileName = "ResistanceProfile")]
  public class DamageResistanceSO : ScriptableObject
  {
    [Serializable]
    public struct Entry
    {
      public DamageType type;
      [Tooltip("Multiplier applied to this damage type.  1 is normal, 0.5 is 50% resist, 1.5 is 50% weak.")]
      public float multiplier;
    }

    public List<Entry> entries = new List<Entry>();

    public float GetMultiplier(DamageType t)
    {
      for (int i = 0; i < entries.Count; i++)
      {
        if (entries[i].type == t) return Mathf.Max(0f, entries[i].multiplier);
      }
      return 1f;
    }
  }

  /// <summary>
  /// Attach to enemies to source multipliers.  Optional, defaults to 1x when absent.
  /// </summary>
  public class DamageResistanceComponent : MonoBehaviour
  {
    public DamageResistanceSO profile;

    public float GetMultiplier(DamageType t)
    {
      return profile != null ? profile.GetMultiplier(t) : 1f;
    }
  }
}
