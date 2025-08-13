using UnityEngine;

[CreateAssetMenu(menuName = "TD/Targeting/Furthest Along Path")]
public class TargetFurthestAlongFlowSO : TargetingPolicySO {
    public override Transform ChooseTarget(TowerShooter shooter, Collider[] candidates, int count) {
        var grid = GridService.Instance;
        if (!grid) return null;

        Transform best = null;
        int bestDist = int.MaxValue; // smaller distance to goal = further along path
        for (int i = 0; i < count; i++) {
            var c = candidates[i]; if (!c) continue;
            var t = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
            var cell = grid.WorldToCell(t.position);
            if (!grid.InBounds(cell)) continue;
            int d = grid.Distance(cell);
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }
}
