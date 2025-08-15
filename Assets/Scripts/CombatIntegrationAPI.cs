using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Simple static API so your existing projectiles or towers can integrate without refactors.
  /// Call ApplyHit once per impact.
  /// </summary>
  public static class CombatIntegrationAPI
  {
    /// <summary>
    /// Applies optional primer tags, then resolves damage with resistances and synergies.
    /// Returns the DamageReport so callers can read extraChain or applied statuses.
    /// </summary>
    public static DamageReport ApplyHit(GameObject source, GameObject target, float baseDamage, DamageType type, StatusTagSO[] primers = null)
    {
      if (target == null) return new DamageReport(0f, null, 0);

      // Apply primers first
      if (primers != null && primers.Length > 0)
      {
        var sc = target.GetComponent<StatusController>();
        if (sc != null)
        {
          for (int i = 0; i < primers.Length; i++)
          {
            var tag = primers[i];
            if (tag != null) sc.Apply(new StatusApplication(tag));
          }
        }
      }

      var ctx = new HitContext(source, target, type, baseDamage);
      return DamageResolver.ResolveAndApply(ctx);
    }
  }
}
