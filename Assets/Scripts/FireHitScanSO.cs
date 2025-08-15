// File: FireHitScanSO.cs
using UnityEngine;
using System.Linq;
using TD.Combat;

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
    public float lineWidth = 0.05f;
    public Material lineMaterial;

    static readonly RaycastHit[] _hitsBuf = new RaycastHit[64];
    static Material _defaultLineMat;

    // Your base class requires this signature per compiler error.
    public override void FireTick(TowerShooter shooter, Transform pivot)
    {
        if (!shooter) return;

        var origin = pivot ? pivot.position
                    : shooter.firePoint ? shooter.firePoint.position
                    : shooter.transform.position;

        var fwd = pivot ? pivot.forward
                 : shooter.firePoint ? shooter.firePoint.forward
                 : shooter.transform.forward;

        int count = 0;
        if (radius <= 0f)
        {
            count = Physics.RaycastNonAlloc(origin, fwd, _hitsBuf, range, hitMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            count = Physics.SphereCastNonAlloc(origin, radius, fwd, _hitsBuf, range, hitMask, QueryTriggerInteraction.Ignore);
        }

        if (count > 0)
        {
            var hits = _hitsBuf.Take(count)
                .OrderBy(h => h.distance)
                .ToArray();

            int remaining = pierceCount <= 0 ? 1 : (pierceCount + 1);
            float dmg = shooter.definition ? shooter.definition.baseDamage : 10f;
            DamageType type = shooter.definition ? shooter.definition.damageType : DamageType.Physical;

            for (int i = 0; i < hits.Length && remaining > 0; i++, remaining--)
            {
                CombatIntegrationAPI.ApplyHit(
                    shooter.gameObject,
                    hits[i].collider,
                    dmg,
                    type,
                    hits[i].point,
                    hits[i].normal
                );
            }
        }

        // Visual
        if (drawLine)
        {
            var go = new GameObject("HitscanLine") { hideFlags = HideFlags.HideAndDontSave };
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin + fwd * range);
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;

            if (lineMaterial != null)
            {
                lr.sharedMaterial = lineMaterial;
            }
            else
            {
                if (_defaultLineMat == null)
                    _defaultLineMat = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
                lr.sharedMaterial = _defaultLineMat;
            }

            lr.textureMode = LineTextureMode.Stretch;
            Object.Destroy(go, lineDuration);
        }
    }
}
