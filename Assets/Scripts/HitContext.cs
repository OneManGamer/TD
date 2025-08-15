using UnityEngine;

namespace TD.Combat
{
    /// <summary>
    /// Immutable context passed alongside every damage event.  Keeps all combat metadata in one place.
    /// </summary>
    public struct HitContext
    {
        public readonly GameObject Source;      // dealer
        public readonly GameObject Target;      // receiver
        public readonly DamageType DamageType;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly bool IsCrit;
        public readonly float CritMultiplier;   // 1.0 means no change
        public readonly object UserData;        // optional bag for tags, tower refs, etc.

        public HitContext(
            GameObject source,
            GameObject target,
            DamageType damageType,
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool isCrit = false,
            float critMultiplier = 1f,
            object userData = null)
        {
            Source = source;
            Target = target;
            DamageType = damageType;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            IsCrit = isCrit;
            CritMultiplier = critMultiplier <= 0f ? 1f : critMultiplier;
            UserData = userData;
        }

        public HitContext WithTarget(GameObject newTarget) =>
            new HitContext(Source, newTarget, DamageType, HitPoint, HitNormal, IsCrit, CritMultiplier, UserData);

        public HitContext WithCrit(bool isCrit, float critMult) =>
            new HitContext(Source, Target, DamageType, HitPoint, HitNormal, isCrit, critMult, UserData);
    }
}
