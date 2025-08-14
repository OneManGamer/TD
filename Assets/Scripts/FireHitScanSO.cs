// File: FireHitScanSO.cs
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "TD/Fire/Hitscan")]
public class FireHitScanSO : FireBehaviourSO
{
    public float range = 12f;
    public float radius = 0f;      // 0 = Raycast, >0 = SphereCast
    public int pierceCount = 0;    // 0 = first hit only, >0 = allow through N hits
    public LayerMask hitMask = ~0;

    [Header("Line FX")]
    public bool drawLine = true;
    public float lineDuration = 0.05f;
    public float lineWidth = 0.035f;
    public Material lineMaterial;

    public override void FireTick(TowerShooter shooter, Transform target)
    {
        if (!shooter) return;

        Vector3 from = shooter.firePoint ? shooter.firePoint.position : shooter.transform.position;
        Vector3 to = target ? target.position + Vector3.up * 0.6f : from + shooter.transform.forward * range;
        Vector3 dir = (to - from).sqrMagnitude > 0.0001f ? (to - from).normalized : shooter.transform.forward;

        RaycastHit[] hits = radius <= 0f
            ? Physics.RaycastAll(from, dir, range, hitMask, QueryTriggerInteraction.Ignore)
            : Physics.SphereCastAll(from, radius, dir, range, hitMask, QueryTriggerInteraction.Ignore);

        var ordered = hits.OrderBy(h => h.distance).ToArray();
        int take = 1 + Mathf.Max(0, pierceCount);
        int taken = 0;

        float dmg = shooter.definition ? shooter.definition.baseDamage : 10f;
        var info = new DamageInfo {
            amount = dmg,
            type = shooter.definition ? shooter.definition.damageType : DamageType.Physical,
            critMult = shooter.RollCrit(),
            source = shooter.gameObject,
            hitPoint = to
        };

        foreach (var h in ordered)
        {
            Combat.ApplyHit(h.collider, info);
            if (++taken >= take) break;
        }

        if (drawLine)
        {
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
