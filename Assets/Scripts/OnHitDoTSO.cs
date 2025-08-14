using UnityEngine;

[CreateAssetMenu(menuName = "TD/Effects/OnHit/DoT")]
public class OnHitDoTSO : OnHitEffectSO
{
    [Header("Damage over Time")]
    public float tickDamage = 3f;
    public int ticks = 5;
    public float tickInterval = 0.5f;
    public DamageType damageType = DamageType.Physical;

    public override void Apply(Collider hitCollider, in DamageInfo context)
    {
        if (!hitCollider) return;
        var go = hitCollider.attachedRigidbody ? hitCollider.attachedRigidbody.gameObject : hitCollider.gameObject;
        var runner = go.GetComponent<DoTAttachment>();
        if (!runner) runner = go.AddComponent<DoTAttachment>();

        runner.StartDoT(tickDamage, ticks, tickInterval, damageType, context.source);
    }
}
