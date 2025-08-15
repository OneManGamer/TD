using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Evaluates primer→detonator rules against the target's active tags.
  /// </summary>
  public static class SynergyService
  {
    private static SynergyDatabaseSO _db;

    public static void SetDatabase(SynergyDatabaseSO db) => _db = db;

    private static SynergyDatabaseSO GetDb()
    {
      if (_db != null) return _db;
      _db = Resources.Load<SynergyDatabaseSO>("TD/SynergyDatabase");
      return _db;
    }

    public struct Outcome
    {
      public float damageMultiplier;
      public float flatBonus;
      public List<StatusApplication> tagsToApply;
      public List<StatusTagSO> primersToConsume;
      public int extraChain;

      public static Outcome Default => new Outcome
      {
        damageMultiplier = 1f,
        flatBonus = 0f,
        tagsToApply = new List<StatusApplication>(4),
        primersToConsume = new List<StatusTagSO>(2),
        extraChain = 0
      };
    }

    public static Outcome Evaluate(StatusController targetStatus, DamageType incomingType)
    {
      var db = GetDb();
      var outcome = Outcome.Default;
      if (db == null || targetStatus == null) return outcome;

      var rules = db.rules;
      if (rules == null || rules.Count == 0) return outcome;

      for (int i = 0; i < rules.Count; i++)
      {
        var r = rules[i];
        if (r == null || r.primerTag == null) continue;
        if (r.detonator != incomingType) continue;
        if (!targetStatus.HasTag(r.primerTag)) continue;

        outcome.damageMultiplier *= Mathf.Max(0f, r.damageMultiplier <= 0f ? 1f : r.damageMultiplier);
        outcome.flatBonus += r.flatBonusDamage;

        if (r.applyOnDetonate != null && r.applyOnDetonate.Length > 0)
          outcome.tagsToApply.AddRange(r.applyOnDetonate);

        if (r.consumePrimerOnDetonate)
          outcome.primersToConsume.Add(r.primerTag);

        if (r.addExtraChain && r.extraChainAmount > 0)
          outcome.extraChain += r.extraChainAmount;
      }

      return outcome;
    }
  }
}
