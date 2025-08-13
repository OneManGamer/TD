using UnityEngine;

[CreateAssetMenu(menuName = "TD/Data/Tower Definition")]
public class TowerDefinitionSO : ScriptableObject {
    [Header("Stats")]
    public string displayName = "Tower";
    public float range = 8f;
    public float shotsPerSecond = 2f;
    public float turnSpeedDegPerSec = 360f;
    public float minFireAngle = 8f;

    [Header("Behaviors")]
    public FireBehaviourSO fireBehaviour;
    public TargetingPolicySO targetingPolicy;

    [Header("Economy")]
    public int buildCost = 60;
}
