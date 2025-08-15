using System;
using UnityEngine;

namespace TD.Combat
{
  [CreateAssetMenu(menuName = "TD/Synergy Rule", fileName = "SynergyRule")]
  public class SynergyRuleSO : ScriptableObject
  {
    [Header("Primer and Detonator")]
    public StatusTagSO primerTag;        // Example: Wet
    public DamageType detonator;         // Example: Shock

    [Header("Damage Modifiers")]
    public float damageMultiplier = 1f;  // Multiplies post-resistance damage
    public float flatBonusDamage = 0f;   // Added after multiplier

    [Header("Behavior Flags")]
    public bool consumePrimerOnDetonate = true;

    [Header("Extra Effects")]
    public StatusApplication[] applyOnDetonate = Array.Empty<StatusApplication>();

    [Header("Optional metadata for other systems")]
    public bool addExtraChain = false;
    public int extraChainAmount = 0;
  }
}
