// File: FireStraightSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Projectile Straight")]
public class FireStraightSO : FireBehaviourSO
{
    [Header("Projectile")]
    public ArrowProjectile projectilePrefab;   // was GameObject
    public float speed = 24f;
    public bool stickOnHit = true;             // toggle in Inspector

    public override void FireTick(TowerShooter shooter, Transform target)
    {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 aimPoint = ComputeAimPoint(shooter, target, origin);

        ApplyMissCone(shooter, origin, ref aimPoint);

        // Spawn via ProjectilePool (fallback to Instantiate if pool is missing)
        ArrowProjectile proj = ProjectilePool.Instance
            ? ProjectilePool.Instance.Spawn(projectilePrefab, origin, Quaternion.identity)
            : Object.Instantiate(projectilePrefab, origin, Quaternion.identity);

        if (!proj)
        {
            Debug.LogError("FireStraightSO: projectile prefab must have ArrowProjectile.");
            return;
        }

        // Configure projectile
        proj.stickOnHit = stickOnHit;

        float dmg = shooter.definition ? shooter.definition.baseDamage : 10f;
        DamageType type = shooter.definition ? shooter.definition.damageType : DamageType.Physical;
        proj.SetDamage(dmg, type);

        // Launch
        proj.LaunchToward(origin, aimPoint, speed);

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }

    // ——— helpers (unchanged) ———
    Vector3 ComputeAimPoint(TowerShooter shooter, Transform target, Vector3 origin)
    {
        Vector3 basePoint = target.position + Vector3.up * 0.6f;

        var def = shooter.definition;
        if (def && def.leadTargets)
        {
            Vector3 vel = GetTargetVelocity(target);
            if (vel.sqrMagnitude > 0.0001f)
            {
                Vector3 leadPoint = PredictIntercept(origin, speed, target.position, vel, def.maxLeadSeconds);
                return leadPoint + Vector3.up * 0.6f;
            }
        }
        return basePoint;
    }

    static Vector3 GetTargetVelocity(Transform t)
    {
        var vt = t.GetComponent<VelocityTracker>();
        if (vt != null) return vt.Velocity;

        var rb = t.GetComponent<Rigidbody>();
        if (rb && !rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        return Vector3.zero;
    }

    static Vector3 PredictIntercept(Vector3 shooterPos, float projSpeed, Vector3 targetPos, Vector3 targetVel, float maxT)
    {
        Vector3 r = targetPos - shooterPos;
        float vv = targetVel.sqrMagnitude;
        float ss = projSpeed * projSpeed;

        float a = vv - ss;
        float b = 2f * Vector3.Dot(r, targetVel);
        float c = r.sqrMagnitude;

        if (Mathf.Abs(a) < 1e-6f)
        {
            float tLin = -c / Mathf.Max(1e-6f, b);
            tLin = Mathf.Clamp(tLin, 0f, maxT);
            return targetPos + targetVel * tLin;
        }

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return targetPos;

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b + sqrt) / (2f * a);
        float t2 = (-b - sqrt) / (2f * a);

        float t = float.PositiveInfinity;
        if (t1 > 0f) t = Mathf.Min(t, t1);
        if (t2 > 0f) t = Mathf.Min(t, t2);
        if (!float.IsFinite(t)) t = 0f;

        t = Mathf.Clamp(t, 0f, maxT);
        return targetPos + targetVel * t;
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
