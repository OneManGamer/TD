using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Watches for Frozen to end (expire) and applies FreezeImmune.
  /// Put this on the same object as StatusController.
  /// </summary>
  public class FrozenImmunityWatcher : MonoBehaviour
  {
    public StatusController controller;
    public float immunitySeconds = 5f;

    bool hadFrozenLastFrame;

    void Reset()
    {
      controller = GetComponentInParent<StatusController>();
    }

    void Update()
    {
      if (!controller) return;

      var frozen = SynergyService.FrozenTag;
      bool hasFrozen = (frozen != null) && controller.Has(frozen);

      if (hadFrozenLastFrame && !hasFrozen)
      {
        // Frozen just ended -> grant immunity
        SynergyService.ApplyFreezeImmunity(controller, immunitySeconds);
      }

      hadFrozenLastFrame = hasFrozen;
    }
  }
}
