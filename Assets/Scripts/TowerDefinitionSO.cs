// File: TowerDefinitionSO.cs
using UnityEngine;
using TD.Combat;

[CreateAssetMenu(fileName = "TowerDefinition", menuName = "TD/Data/Tower Definition")]
public class TowerDefinitionSO : ScriptableObject
{
    [Header("Core")]
    public float baseDamage = 10f;
    public DamageType damageType = DamageType.Physical;
    public float shotsPerSecond = 1.5f;
    public float range = 10f;
    public float turnSpeedDegPerSec = 360f;
    [Range(0f, 360f)] public float minFireAngle = 360f;

    [Header("Crits")]
    [Range(0f, 1f)] public float critChance = 0f;
    public float critMultiplier = 2f;

    [Header("Behaviour")]
    public FireBehaviourSO fireBehaviour;
    public TargetingPolicySO targetingPolicy;

    [Header("Accuracy")]
    [Tooltip("Chance [0..1] that we intentionally offset the aim to force a miss.")]
    [Range(0f, 1f)] public float missChance = 0f;
    [Tooltip("Cone half-angle (degrees) used when a miss happens.")]
    [Range(0f, 30f)] public float missAngleDegrees = 0f;

    [Tooltip("Try to lead moving targets (reduces natural misses).")]
    public bool leadTargets = true;
    [Tooltip("Hard clamp on lead time to avoid extreme overshoot.")]
    [Range(0f, 1.5f)] public float maxLeadSeconds = 0.6f;

    [Header("Economy")]
    public AmmoDefinitionSO ammo;   // optional
    public int buildCost = 60;
    public int sellValue = 30;
    public float buildTime = 1.5f;

    void OnValidate()
    {
        baseDamage = Mathf.Max(0f, baseDamage);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        range = Mathf.Max(0.1f, range);
        turnSpeedDegPerSec = Mathf.Max(0f, turnSpeedDegPerSec);
        minFireAngle = Mathf.Clamp(minFireAngle, 0f, 360f);
        critMultiplier = Mathf.Max(1f, critMultiplier);
        buildCost = Mathf.Max(0, buildCost);
        sellValue = Mathf.Clamp(sellValue, 0, buildCost);
        buildTime = Mathf.Max(0f, buildTime);

        missChance = Mathf.Clamp01(missChance);
        missAngleDegrees = Mathf.Clamp(missAngleDegrees, 0f, 30f);
        maxLeadSeconds = Mathf.Clamp(maxLeadSeconds, 0f, 1.5f);
    }
}
