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

    [Header("Primers (optional)")]
    [SerializeField] private StatusTagSO[] primers; // not used by the core API, but passed through UserData if your on-hit effects read it

    [Header("Auto trigger/collision")]
    [SerializeField] private bool useTriggerEnter = true;
    [SerializeField] private bool useCollisionEnter = false;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Attacker")]
    [SerializeField] private GameObject attackerOverride;       // Optional.  If null, uses this.gameObject as the attacker

    /// <summary>
    /// Call this from your own hit logic if you do raycasts or custom detection.
    /// Returns true if damage was applied to a valid IDamageable.
    /// </summary>
    public bool ApplyToTarget(GameObject target)
    {
      if (target == null) return false;
      var attacker = attackerOverride != null ? attackerOverride : gameObject;

      // We pass primers via HitContext.UserData for any systems that care.
      var ctx = new HitContext(attacker, target, damageType, target.transform.position, Vector3.up, false, 1f, primers);
      return CombatIntegrationAPI.ApplyHit(attacker, target, baseDamage, damageType, in ctx);
    }

    private void OnTriggerEnter(Collider other)
    {
      if (!useTriggerEnter) return;
      TryApply(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (!useCollisionEnter) return;
      TryApply(collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject);
    }

    private void TryApply(GameObject hit)
    {
      if (hit == null) return;

      // Only process if the target can actually be damaged
      var damageable = hit.GetComponentInParent<IDamageable>();
      if (damageable == null) return;

      ApplyToTarget(hit);

      if (destroyOnHit) Destroy(gameObject);
    }
  }
}
