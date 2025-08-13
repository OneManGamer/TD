using UnityEngine;

public class TowerTargeting : MonoBehaviour {
    [Header("Targeting")]
    public float range = 7f;
    public string enemyTag = "Enemy";
    public float retargetCooldown = 0.25f;

    [Tooltip("Assigned at runtime; null if no valid target in range.")]
    public Transform currentTarget;

    float _timer;

    void Update() {
        _timer -= Time.deltaTime;
        if (_timer <= 0f) {
            _timer = retargetCooldown;
            MaintainOrFindTarget();
        }
        if (currentTarget == null) currentTarget = null;
    }

    void MaintainOrFindTarget() {
        Vector3 p = transform.position;

        // keep current if still valid
        if (currentTarget != null) {
            if ((currentTarget.position - p).sqrMagnitude <= range * range) return;
            currentTarget = null;
        }

        // find closest enemy in range
        float bestSqr = float.MaxValue;
        Transform best = null;
        var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (var e in enemies) {
            if (e == null) continue;
            float d2 = (e.transform.position - p).sqrMagnitude;
            if (d2 <= range * range && d2 < bestSqr) {
                bestSqr = d2; best = e.transform;
            }
        }
        currentTarget = best;
    }

    public bool HasTarget => currentTarget != null;

    public Vector3 AimPoint() {
        if (!currentTarget) return transform.position;
        return currentTarget.position + Vector3.up * 0.6f; // aim at center mass
    }
}
