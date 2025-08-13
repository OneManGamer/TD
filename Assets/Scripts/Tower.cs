using UnityEngine;

public class Tower : MonoBehaviour {
    public float range = 6f;
    public float fireRatePerSec = 1.2f;
    public Projectile projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 12f;

    Transform lockedTarget;
    float fireCooldown;

    void Update() {
        if (lockedTarget == null || !IsValid(lockedTarget)) {
            lockedTarget = AcquireClosest();
        }
        if (lockedTarget != null) {
            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f) {
                Fire(lockedTarget);
                fireCooldown = 1f / fireRatePerSec;
            }
        }
    }

    bool IsValid(Transform t) {
        if (t == null) return false;
        return Vector3.Distance(transform.position, t.position) <= range && t.GetComponent<Health>() != null;
    }

    Transform AcquireClosest() {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 pos = transform.position;
        foreach (var e in enemies) {
            float d = Vector3.Distance(pos, e.transform.position);
            if (d <= range && d < bestDist) { best = e.transform; bestDist = d; }
        }
        return best;
    }

    void Fire(Transform t) {
        if (projectilePrefab == null || firePoint == null) return;
        var p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        p.Init(t, projectileDamage);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
