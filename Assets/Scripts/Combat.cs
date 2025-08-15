using System;
using UnityEngine;

namespace TD.Combat
{
    public static class Combat
    {
        [Obsolete("Use CombatIntegrationAPI.ApplyHit(...) with HitContext.")]
        public static bool ApplyHit(Collider target, DamageInfo info)
        {
            if (info == null || target == null) return false;

            return CombatIntegrationAPI.ApplyHit(
                info.Source,
                target,
                info.Amount,
                info.Type,
                info.HitPoint,
                info.HitNormal,
                info.IsCrit,
                info.CritMultiplier,
                info.UserData
            );
        }
    }

    // Minimal container so legacy code still compiles until migrated.
    public class DamageInfo
    {
        public GameObject Source;
        public float Amount;
        public DamageType Type;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public bool IsCrit;
        public float CritMultiplier = 1f;
        public object UserData;
    }
}
