using System.Collections.Generic;
using UnityEngine;
using TD.Combat;

[CreateAssetMenu(menuName = "TD/Fire/Projectile Ballistic")]
public class FireBallisticSO : FireBehaviourSO
{
    [Header("Projectile")]
    public ArrowProjectile projectilePrefab;

    [Header("Ballistics")]
    public float launchSpeed = 18f;
    public float gravity = 9.81f;
    public bool highArc = false;
    public bool stickOnHit = true;

    [Header("Aim")]
    public float aimYOffset = 0.6f;

    [Header("On-Hit Effects")]
    public List<OnHitEffectSO> onHitEffects;

    public override void FireTick(TowerShooter shooter, Transform target)
    {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 aim = target.position + Vector3.up * aimYOffset;

        // Apply intentional miss if requested by the definition
        ApplyMissCone(shooter, origin, ref aim);

        // Pool-aware spawn (fallback to Instantiate)
        ArrowProjectile proj = ProjectilePool.Instance
            ? ProjectilePool.Instance.Spawn(projectilePrefab, origin, Quaternion.identity)
            : Object.Instantiate(projectilePrefab, origin, Quaternion.identity);

        if (!proj)
        {
            Debug.LogError("FireBallisticSO: projectile prefab must have ArrowProjectile.");
            return;
        }

        proj.stickOnHit = stickOnHit;

        float dmg = shooter.definition ? shooter.definition.baseDamage : 10f;
        DamageType type = shooter.definition ? shooter.definition.damageType : DamageType.Physical;
        proj.SetDamage(dmg, type);

        // Data-driven effects
        proj.SetOnHitEffects(onHitEffects);

        // Try ballistic first; if no solution, fire straight
        bool ok = proj.TryLaunchBallistic(origin, aim, launchSpeed, gravity, highArc);
        if (!ok) proj.LaunchToward(origin, aim, launchSpeed);

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }

    void ApplyMissCone(TowerShooter shooter, Vector3 origin, ref Vector3 aimPoint)
    {
        var def = shooter.definition;
        if (!def || def.missChance <= 0f || def.missAngleDegrees <= 0f) return;
        if (Random.value > def.missChance) return;

        Vector3 dir = (aimPoint - origin);
        float dist = dir.magnitude;
        if (dist < 0.001f) return;

        dir /= dist;

        float rad = Mathf.Tan(def.missAngleDegrees * Mathf.Deg2Rad);
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(dir, right);

        Vector2 r = Random.insideUnitCircle * rad;
        Vector3 missDir = (dir + right * r.x + up * r.y).normalized;

        aimPoint = origin + missDir * dist;
    }
}
