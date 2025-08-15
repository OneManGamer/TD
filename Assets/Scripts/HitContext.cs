using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Immutable input describing a single hit.
  /// </summary>
  public struct HitContext
  {
    public GameObject source;
    public GameObject target;
    public DamageType damageType;
    public float baseDamage;

    public HitContext(GameObject source, GameObject target, DamageType type, float baseDamage)
    {
      this.source = source;
      this.target = target;
      this.damageType = type;
      this.baseDamage = baseDamage;
    }
  }

  /// <summary>
  /// Result from resolving a hit.
  /// </summary>
  public struct DamageReport
  {
    public float finalDamage;
    public StatusApplication[] appliedStatuses;
    public int extraChain;

    public DamageReport(float finalDamage, StatusApplication[] applied, int extraChain)
    {
      this.finalDamage = finalDamage;
      this.appliedStatuses = applied;
      this.extraChain = extraChain;
    }
  }
}
