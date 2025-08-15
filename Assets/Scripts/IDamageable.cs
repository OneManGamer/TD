namespace TD.Combat
{
  /// <summary>
  /// Implement on targets that can take damage.
  /// </summary>
  public interface IDamageable
  {
    void ApplyDamage(float amount, HitContext ctx);
  }
}
