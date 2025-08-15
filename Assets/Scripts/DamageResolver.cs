using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Central one-shot resolver you call from your projectile or tower on hit.
  /// </summary>
  public static class DamageResolver
  {
    public static DamageReport ResolveAndApply(HitContext ctx)
    {
      if (ctx.target == null) return new DamageReport(0f, null, 0);

      var status = ctx.target.GetComponent<StatusController>();
      var resist = ctx.target.GetComponent<DamageResistanceComponent>();
      var damageable = ctx.target.GetComponent<IDamageable>();

      float dmg = Mathf.Max(0f, ctx.baseDamage);

      // 1) Resistances
      float mult = resist != null ? resist.GetMultiplier(ctx.damageType) : 1f;
      dmg *= mult;

      // 2) Synergies
      var outcome = SynergyService.Evaluate(status, ctx.damageType);
      dmg = dmg * outcome.damageMultiplier + outcome.flatBonus;

      // Clamp
      dmg = Mathf.Max(0f, dmg);

      // Apply damage
      if (damageable != null) damageable.ApplyDamage(dmg, ctx);

      // Consume primers and apply new tags
      var applied = new List<StatusApplication>(outcome.tagsToApply);
      if (status != null)
      {
        for (int i = 0; i < outcome.primersToConsume.Count; i++)
          status.ConsumeTag(outcome.primersToConsume[i]);

        status.ApplyMany(applied);
      }

      return new DamageReport(dmg, applied.ToArray(), outcome.extraChain);
    }
  }
}
