using UnityEngine;

[CreateAssetMenu(menuName = "TD/Effects/OnHit/Splash")]
public class OnHitSplashSO : OnHitEffectSO
{
    [Header("Splash")]
    public float radius = 2.5f;
    [Tooltip("Splash damage as a fraction of the main hit's final damage.")]
    public float damageFraction = 0.6f;
    public LayerMask hitMask = ~0;
    public bool includeOriginalTarget = false;

    static readonly Collider[] _buf = new Collider[64];

    public override void Apply(Collider hitCollider, in DamageInfo context)
    {
        if (!hitCollider) return;
        var center = context.hitPoint; // set by projectile
        int n = Physics.OverlapSphereNonAlloc(center, Mathf.Max(0.01f, radius), _buf, hitMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            var col = _buf[i];
            if (!col) continue;
            if (!includeOriginalTarget && col == hitCollider) continue;

            var info = new DamageInfo
            {
                amount = Mathf.Max(0f, context.amount * damageFraction),
                type = context.type,
                critMult = 1f,
                source = context.source,
                hitPoint = col.ClosestPoint(center)
            };
            Combat.ApplyHit(col, info);
        }
    }
}
