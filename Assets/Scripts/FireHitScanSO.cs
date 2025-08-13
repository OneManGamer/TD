using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "TD/Fire/Hitscan (Laser/Rail)")]
public class FireHitScanSO : FireBehaviourSO {
    public float damage = 10f;
    public int pierceCount = 0;
    public float range = 12f;
    public float radius = 0f;
    public bool drawLine = true;
    public float lineDuration = 0.05f;
    public float lineWidth = 0.035f;
    public Material lineMaterial;
    public LayerMask hitMask = ~0;

    public override void FireTick(TowerShooter shooter, Transform target) {
        if (!shooter || !shooter.firePoint) return;

        Vector3 from = shooter.firePoint.position;
        Vector3 to = target ? target.position + Vector3.up * 0.6f : (from + shooter.firePoint.forward * range);
        Vector3 dir = (to - from).sqrMagnitude > 0.0001f ? (to - from).normalized : shooter.firePoint.forward;

        var hits = (radius > 0f)
            ? Physics.SphereCastAll(from, radius, dir, range, hitMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(from, dir, range, hitMask, QueryTriggerInteraction.Ignore);

        int take = pierceCount + 1, taken = 0;
        foreach (var h in hits.OrderBy(h => h.distance)) {
            var health = h.collider.GetComponentInParent<Health>();
            if (health) { health.TakeDamage(damage); if (++taken >= take) break; }
        }

        if (drawLine) {
            var go = new GameObject("HitscanLine");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new[] { from, from + dir * range });
            lr.startWidth = lr.endWidth = lineWidth;
            lr.material = lineMaterial ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.textureMode = LineTextureMode.Stretch;
            Object.Destroy(go, lineDuration);
        }
    }
}
