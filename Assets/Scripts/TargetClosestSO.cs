using UnityEngine;

[CreateAssetMenu(menuName = "TD/Targeting/Closest To Tower")]
public class TargetClosestSO : TargetingPolicySO {
    public override Transform ChooseTarget(TowerShooter shooter, Collider[] candidates, int count) {
        Transform best = null;
        float bestD2 = float.MaxValue;
        Vector3 p = shooter.transform.position;
        for (int i = 0; i < count; i++) {
            var c = candidates[i]; if (!c) continue;
            var t = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
            float d2 = (t.position - p).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = t; }
        }
        return best;
    }
}
