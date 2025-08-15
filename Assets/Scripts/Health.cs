// File: Health.cs
using UnityEngine;
using System;
using TD.Combat;

public class Health : MonoBehaviour, IDamageable
{
  [Serializable]
  public struct Resist { public DamageType type; public float multiplier; }  // 1 = normal, 0.5 = resist, 2 = weak

  [Header("Stats (runtime)")]
  [Tooltip("If an EnemyDefinitionSO is applied at spawn, maxHP/currentHP will be overwritten by that definition.")]
  public float maxHP = 100f;
  public float currentHP = 100f;

  [Tooltip("Runtime copy.  If a definition is applied, this is set from that asset.  Ignored by the new resolver when DamageResistanceComponent is present.")]
  public Resist[] resistances;

  public event Action OnDeath;

  void Awake()
  {
    if (currentHP <= 0f) currentHP = maxHP;
    currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
  }

  public void InitFrom(EnemyDefinitionSO def, bool fillToMax = true)
  {
    if (!def) return;
    maxHP = Mathf.Max(1f, def.maxHP);
    if (fillToMax) currentHP = maxHP;
    resistances = def.resistances;
  }

  float Mult(DamageType t)
  {
    if (resistances == null) return 1f;
    for (int i = 0; i < resistances.Length; i++)
      if (resistances[i].type == t) return Mathf.Max(0f, resistances[i].multiplier);
    return 1f;
  }

  // Legacy path kept for compatibility with old callers.
  // Prefer the new resolver which calls the IDamageable method below.
  public void ApplyDamage(in DamageInfo info)
  {
    float amt = info.amount * Mult(info.type) * Mathf.Max(1f, info.critMult);
    if (amt <= 0f) return;
    currentHP = Mathf.Max(0f, currentHP - amt);
    if (currentHP <= 0f) Die();
  }

  // New resolver path required by TD.Combat.IDamageable.
  // Amount is already final after resistances and synergies.  Do not reapply multipliers here.
  public void ApplyDamage(float amount, HitContext ctx)
  {
    if (amount <= 0f) return;
    currentHP = Mathf.Max(0f, currentHP - amount);
    if (currentHP <= 0f) Die();
  }

  void Die()
  {
    currentHP = 0f;
    OnDeath?.Invoke();
    gameObject.SetActive(false);
  }
}
