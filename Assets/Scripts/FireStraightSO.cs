using UnityEngine;

[CreateAssetMenu(menuName = "TD/Fire/Projectile Straight")]
public class FireStraightSO : FireBehaviourSO {
    public GameObject projectilePrefab; // prefab with ArrowProjectile on root
    public float speed = 24f;

    public override void FireTick(TowerShooter shooter, Transform target) {
        if (!projectilePrefab || !shooter || !shooter.firePoint || !target) return;

        Vector3 origin = shooter.firePoint.position;
        Vector3 aim = target.position + Vector3.up * 0.6f;

        GameObject go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<ArrowProjectile>(); if (!proj) { Debug.LogError("Projectile prefab missing ArrowProjectile."); return; }

        proj.LaunchToward(origin, aim, speed);
        proj.stickOnHit = false;

        if (shooter.sfxShot) shooter.sfxShot.Play();
    }
}
