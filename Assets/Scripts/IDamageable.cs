namespace TD.Combat
{
    public interface IDamageable
    {
        /// <summary>
        /// Apply already resolved damage to this object with context for VFX, events, and logs.  Amount will be clamped by the callee as needed.
        /// </summary>
        void ApplyDamage(float amount, HitContext context);
    }
}
