using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Shotgun")]
public class FireShotgunSO : FireBehaviourSO
{
    [Header("Projectile")]
    public ArrowProjectile projectilePrefab;
    public int pellets = 6;
    public float speed = 22f;
    public bool stickOnHit = false;

    [Header("Spread")]
    [Tooltip("Cone half-angle in degrees.")]
    public float spreadDegrees = 6f;
    [Tooltip("Vertical aim offset applied to target, in meters.")]
    public float aimYOffset = 0.6f;

    [Header("Damage")]
    [Tooltip("Damage multiplier per pellet relative to tower baseDamage.")]
    public float pelletDamageMultiplier = 0.35f;

    [Header("On-Hit Effects")]
    public List<OnHitEffectSO> onHitEffects;

    public override void FireTick(TowerShooter shooter, Transform target)
    {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 targetPos = target.position + Vector3.up * aimYOffset;

        // Base direction toward target
        Vector3 forward = (targetPos - origin);
        if (forward.sqrMagnitude < 0.0001f) forward = shooter.transform.forward;
        forward.Normalize();

        // Build an orthonormal basis around the forward direction
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(forward, right);

        // lateral offset factor for the cone
        float rad = Mathf.Tan(Mathf.Max(0f, spreadDegrees) * Mathf.Deg2Rad);

        int count = Mathf.Max(1, pellets);
        for (int i = 0; i < count; i++)
        {
            // Random offset inside unit circle, scaled by cone angle
            Vector2 r = Random.insideUnitCircle * rad;
            Vector3 dir = (forward + right * r.x + up * r.y).normalized;

            // Pool-aware spawn (fallback to Instantiate)
            ArrowProjectile proj = ProjectilePool.Instance
                ? ProjectilePool.Instance.Spawn(projectilePrefab, origin, Quaternion.identity)
                : Object.Instantiate(projectilePrefab, origin, Quaternion.identity);

            if (!proj)
            {
                Debug.LogError("FireShotgunSO: projectile prefab must have ArrowProjectile.");
                continue;
            }

            // Behaviour toggles
            proj.stickOnHit = stickOnHit;

            // Per-pellet damage (includes crit if your shooter supports it)
            float baseDmg = shooter.definition ? shooter.definition.baseDamage : 10f;
            float crit = shooter.RollCrit(); // e.g., 1f for no-crit or >1f for crit
            float pelletDmg = baseDmg * pelletDamageMultiplier * Mathf.Max(1f, crit);
            DamageType type = shooter.definition ? shooter.definition.damageType : DamageType.Physical;
            proj.SetDamage(pelletDmg, type);

            // Data-driven effects
            proj.SetOnHitEffects(onHitEffects);

            // LaunchToward with aimpoint computed from our spread direction
            proj.LaunchToward(origin, origin + dir, speed);
        }

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }
}
