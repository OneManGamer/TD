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
    public Material lineMaterial;       // assign in inspector to avoid defaulting
    static Material _defaultLineMat;    // cached once, no per-shot alloc

    public override void FireTick(TowerShooter shooter, Transform target)
    {
        if (!shooter || !shooter.firePoint || !target) return;

        Vector3 from = shooter.firePoint.position;
        Vector3 dir  = (target.position + Vector3.up * 0.6f - from).normalized;

        RaycastHit[] hits;
        int hitCount = 0;

        if (radius <= 0f)
        {
            if (Physics.Raycast(from, dir, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                hits = new[] { hit };
                hitCount = 1;
            }
            else
            {
                hits = System.Array.Empty<RaycastHit>();
            }
        }
        else
        {
            hits = Physics.SphereCastAll(from, radius, dir, range, hitMask, QueryTriggerInteraction.Ignore)
                   .OrderBy(h => h.distance).ToArray();
            hitCount = hits.Length;
        }

        if (hitCount > 0)
        {
            int remaining = (pierceCount <= 0) ? 1 : (pierceCount + 1);
            for (int i = 0; i < hitCount && remaining > 0; i++, remaining--)
            {
                var info = new DamageInfo
                {
                    amount = shooter.definition ? shooter.definition.baseDamage : 10f,
                    type = shooter.definition ? shooter.definition.damageType : DamageType.Physical,
                    critMult = shooter.RollCrit(), // 1f or >1f
                    source = shooter.gameObject,
                    hitPoint = hits[i].point
                };
                Combat.ApplyHit(hits[i].collider, info);
            }
        }

        // Visual
        if (drawLine)
        {
            var go = new GameObject("HitscanLine");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new[] { from, from + dir * range });
            lr.startWidth = lr.endWidth = lineWidth;

            // Use assigned material or a cached default (no per-shot new Material/Shader.Find)
            if (lineMaterial)
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
