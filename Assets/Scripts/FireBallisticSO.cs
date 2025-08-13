using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Projectile Ballistic")]
public class FireBallisticSO : FireBehaviourSO {
    public GameObject projectilePrefab;
    public float launchSpeed = 18f;
    public float gravity = 9.81f;
    public bool highArc = false;

    public override void FireTick(TowerShooter shooter, Transform target) {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 aim = target.position + Vector3.up * 0.6f;

        GameObject go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<ArrowProjectile>(); if (!proj) { Debug.LogError("Projectile prefab missing ArrowProjectile."); return; }

        proj.gravity = gravity;
        bool ok = proj.TryLaunchBallistic(origin, aim, launchSpeed, gravity, highArc);
        if (!ok) proj.LaunchToward(origin, aim, launchSpeed);
        proj.stickOnHit = true;

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }
}
