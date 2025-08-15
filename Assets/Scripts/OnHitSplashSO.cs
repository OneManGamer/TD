using UnityEngine;
using TD.Combat;

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
        var center = context.hitPoint;
        int count = Physics.OverlapSphereNonAlloc(center, radius, _buf, hitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var col = _buf[i];
            if (!col) continue;
            if (!includeOriginalTarget && col == hitCollider) continue;

            var amount = Mathf.Max(0f, context.amount * damageFraction);
            var hp = col.GetComponentInParent<IDamageable>();
            if (hp == null) continue;

            var closest = col.ClosestPoint(center);
            CombatIntegrationAPI.ApplyHit(
                context.source,
                col,
                amount,
                context.type,
                closest,
                Vector3.up
            );
        }
    }
}
