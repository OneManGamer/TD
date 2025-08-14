// File: OnHitApplyStatusSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Effects/OnHit/Apply Status")]
public class OnHitApplyStatusSO : OnHitEffectSO
{
    [Header("Status to Apply")]
    public StatusEffectSO effect;

    [Header("Optional Overrides")]
    [Tooltip("If true, use this duration instead of the StatusEffect asset's duration.")]
    public bool overrideDuration = false;
    public float durationOverride = 2f;

    [Header("Convenience")]
    [Tooltip("If the enemy lacks Stats/Status components, add them at runtime for testing.")]
    public bool autoAddIfMissing = true;

    public override void Apply(Collider hitCollider, in DamageInfo context)
    {
        if (!hitCollider || !effect) return;

        // Find host (prefer Health root)
        var go = hitCollider.attachedRigidbody ? hitCollider.attachedRigidbody.gameObject : hitCollider.gameObject;
        var health = go ? go.GetComponentInParent<Health>() : null;
        if (!health) return; // no health -> not an enemy

        var host = health.gameObject;

        // Enforce enemy-only (requires your enemy prefabs be tagged "Enemy")
        if (!host.CompareTag("Enemy")) return;

        // Ensure required components (enemy-only)
        var stats = host.GetComponent<StatsComponent>();
        var runtime = host.GetComponent<StatusRuntime>();
        if (autoAddIfMissing)
        {
            if (!stats) stats = host.AddComponent<StatsComponent>();
            if (!runtime) runtime = host.AddComponent<StatusRuntime>();
        }
        if (!runtime) return;

        float? dur = overrideDuration ? Mathf.Max(0f, durationOverride) : (float?)null;

        // Apply non-stacking (refresh) via StatusRuntime
        runtime.Apply(effect, context.source, dur);
    }
}
