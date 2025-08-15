using UnityEngine;

namespace TD.Combat
{
    /// <summary>
    /// Single entry point for dealing damage.  Keeps behavior consistent across projectiles, DoTs, splash, beams, and hitscan.
    /// </summary>
    public static class CombatIntegrationAPI
    {
        /// <summary>
        /// Main entry when you already know the target GameObject.
        /// </summary>
        public static bool ApplyHit(GameObject source, GameObject target, float baseDamage, DamageType type, in HitContext ctx)
        {
            if (!target) return false;

            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null) return false;

            float finalDamage = baseDamage;

            // Crits first so they flow through one place.
            if (ctx.IsCrit && ctx.CritMultiplier > 1f)
                finalDamage *= ctx.CritMultiplier;

            // Optional resistance if present.
            var resist = target.GetComponentInParent<DamageResistanceComponent>();
            if (resist != null)
                finalDamage *= resist.GetMultiplier(type);

            if (finalDamage < 0f) finalDamage = 0f;

            // Ensure context points at the exact instance we are damaging.
            var useCtx = ctx.Target == target ? ctx : ctx.WithTarget(target);

            damageable.ApplyDamage(finalDamage, useCtx);
            return true;
        }

        /// <summary>
        /// Convenience overload with a collider from raycasts or overlaps.
        /// </summary>
        public static bool ApplyHit(
            GameObject source,
            Collider targetCol,
            float baseDamage,
            DamageType type,
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool isCrit = false,
            float critMult = 1f,
            object userData = null)
        {
            if (!targetCol) return false;

            var target = targetCol.attachedRigidbody
                ? targetCol.attachedRigidbody.gameObject
                : targetCol.gameObject;

            var ctx = new HitContext(source, target, type, hitPoint, hitNormal, isCrit, critMult, userData);
            return ApplyHit(source, target, baseDamage, type, in ctx);
        }
    }
}
