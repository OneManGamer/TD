// File: Combat.cs
using UnityEngine;

public static class Combat
{
    public static void ApplyHit(Collider col, in DamageInfo info)
    {
        if (!col) return;
        if (col.TryGetComponent<IDamageable>(out var d)) d.ApplyDamage(info);
    }
}
