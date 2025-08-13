using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Shotgun (Multi-Projectile Spread)")]
public class FireShotgunSO : FireBehaviourSO {
    public GameObject projectilePrefab;
    public int pellets = 5;
    public float spreadAngleDeg = 15f;
    public float speed = 20f;

    public override void FireTick(TowerShooter shooter, Transform target) {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 baseDir = (target.position + Vector3.up * 0.6f - origin);
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.0001f) baseDir = shooter.firePoint.forward;
        baseDir.Normalize();

        for (int i = 0; i < pellets; i++) {
            float yaw = Random.Range(-spreadAngleDeg, spreadAngleDeg);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * baseDir;

            GameObject go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
            var proj = go.GetComponent<ArrowProjectile>(); if (!proj) continue;

            proj.Launch(origin, dir * speed, ArrowProjectile.FlightMode.Straight);
            proj.stickOnHit = false;
        }
    }
}
