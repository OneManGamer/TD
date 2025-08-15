using UnityEngine;

namespace TD.Combat
{
  public class ExampleHealth : MonoBehaviour, IDamageable
  {
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public void ApplyDamage(float amount, HitContext ctx)
    {
      currentHealth = Mathf.Max(0f, currentHealth - amount);
      // Hook your death events, VFX, etc. here.
      if (currentHealth <= 0f)
      {
        // Die.
      }
    }
  }
}
