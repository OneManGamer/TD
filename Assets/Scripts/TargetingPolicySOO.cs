using UnityEngine;

public abstract class TargetingPolicySO : ScriptableObject {
    public abstract Transform ChooseTarget(TowerShooter shooter, Collider[] candidates, int count);
}
