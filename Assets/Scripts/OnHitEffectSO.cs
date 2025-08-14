using UnityEngine;

public abstract class OnHitEffectSO : ScriptableObject
{
    [Range(0f, 1f)] public float chance = 1f;

    /// <summary>Called after the primary hit damage is applied.</summary>
    public abstract void Apply(Collider hitCollider, in DamageInfo context);
}
