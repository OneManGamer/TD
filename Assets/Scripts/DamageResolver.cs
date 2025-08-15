using UnityEngine;

namespace TD.Combat
{
    /// <summary>
    /// Placeholder for future global resolution.  Not currently used by CombatIntegrationAPI.
    /// </summary>
    public class DamageResolver : MonoBehaviour
    {
        public virtual float Resolve(float amount, DamageType type, HitContext ctx)
        {
            return amount;
        }
    }
}
