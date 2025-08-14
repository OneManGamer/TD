// File: FireShotgunSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Shotgun")]
public class FireShotgunSO : FireBehaviourSO
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
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
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
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

            GameObject go = SimplePool.Instance
                ? SimplePool.Instance.Get(projectilePrefab, origin, Quaternion.LookRotation(dir, Vector3.up))
                : Object.Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir, Vector3.up));

            var proj = go.GetComponent<ArrowProjectile>();
            if (!proj)
            {
                Debug.LogError("Projectile prefab missing ArrowProjectile.");
                if (!SimplePool.Instance) Object.Destroy(go);
                continue;
            }

            // Pool bookkeeping & behaviour toggles
            proj.sourcePrefab = projectilePrefab;
            proj.stickOnHit = stickOnHit;

            // Damage now set via helper (tower is the source of truth)
            float baseDmg = shooter.definition ? shooter.definition.baseDamage : 10f;
            float crit = shooter.RollCrit(); // returns 1f or a multiplier (e.g., 2f)
            float pelletDmg = baseDmg * pelletDamageMultiplier * Mathf.Max(1f, crit);
            DamageType type = shooter.definition ? shooter.definition.damageType : DamageType.Physical;
            proj.SetDamage(pelletDmg, type);

            // LaunchToward expects an aim point; using origin + dir preserves our spread direction
            proj.LaunchToward(origin, origin + dir, speed);
        }

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }
}
