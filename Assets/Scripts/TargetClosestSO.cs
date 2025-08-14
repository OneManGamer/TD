// File: TargetClosestSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Targeting/Closest")]
public class TargetClosestSO : TargetingPolicySO
{
    public override Transform ChooseTarget(TowerShooter shooter, Collider[] cands, int count)
    {
        if (!shooter) return null;
        Vector3 origin = shooter.transform.position;
        Transform best = null;
        float bestD2 = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var c = cands[i]; if (!c) continue;
            Transform t = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
            float d2 = (t.position - origin).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = t; }
        }
        return best;
    }
}
