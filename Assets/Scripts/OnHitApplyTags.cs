using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Assign primer tags to this component and call ApplyTags(target) when a hit is confirmed.
  /// </summary>
  public class OnHitApplyTags : MonoBehaviour
  {
    [SerializeField] private StatusTagSO[] tags;

    public void ApplyTags(GameObject target)
    {
      if (target == null || tags == null) return;
      var status = target.GetComponent<StatusController>();
      if (!status) return;

      for (int i = 0; i < tags.Length; i++)
      {
        if (tags[i] != null) status.Apply(new StatusApplication(tags[i]));
      }
    }
  }
}
