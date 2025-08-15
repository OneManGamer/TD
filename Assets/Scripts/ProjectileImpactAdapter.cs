using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Drop-in bridge for projectiles.  Put this on your projectile prefab.
  /// It can auto-apply on trigger or collision, or you can call ApplyToTarget() manually.
  /// </summary>
  public class ProjectileImpactAdapter : MonoBehaviour
  {
    [Header("Damage")]
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private float baseDamage = 10f;

    [Header("Primers (status tags to apply before damage)")]
    [SerializeField] private StatusTagSO[] primers;

    [Header("Hit Detection Options")]
    [SerializeField] private bool useTriggerEnter = true;       // If your projectile uses a trigger collider
    [SerializeField] private bool useCollisionEnter = false;    // If your projectile uses non-trigger collision
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private GameObject attackerOverride;       // Optional.  If null, uses this.gameObject as the attacker

    /// <summary>
    /// Call this from your own hit logic if you do raycasts or custom detection.
    /// </summary>
    public DamageReport ApplyToTarget(GameObject target)
    {
      if (target == null) return new DamageReport(0f, null, 0);

      var attacker = attackerOverride != null ? attackerOverride : gameObject;
      return CombatIntegrationAPI.ApplyHit(attacker, target, baseDamage, damageType, primers);
    }

    private void OnTriggerEnter(Collider other)
    {
      if (!useTriggerEnter) return;
      TryApply(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (!useCollisionEnter) return;
      var hitGO = collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject;
      TryApply(hitGO);
    }

    private void TryApply(GameObject hit)
    {
      if (hit == null) return;

      // Only process if the target can actually be damaged
      var damageable = hit.GetComponent<IDamageable>();
      if (damageable == null) return;

      ApplyToTarget(hit);

      if (destroyOnHit) Destroy(gameObject);
    }
  }
}
