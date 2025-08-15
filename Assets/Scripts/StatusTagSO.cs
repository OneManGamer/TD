using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Data-only status tag definition.  Other systems decide what a tag does.
  /// </summary>
  [CreateAssetMenu(menuName = "TD/Status/Status Tag", fileName = "StatusTag")]
  public class StatusTagSO : ScriptableObject
  {
    [Tooltip("Unique ID for saves and lookups.")]
    public string TagId = "Tag";

    [Tooltip("Default duration in seconds when no override is provided.")]
    public float DefaultDuration = 3f;

    [Tooltip("If true, multiple stacks can be applied.")]
    public bool Stackable = false;

    [Tooltip("Maximum stacks if stackable.")]
    public int MaxStacks = 1;

    [Tooltip("Optional exclusivity group.  Tags in the same group replace each other.")]
    public string ExclusiveGroup = "";
  }

  /// <summary>
  /// Runtime application request for a tag.
  /// </summary>
  public struct StatusApplication
  {
    public StatusTagSO tag;
    public float durationOverride;
    public int stacks;

    public StatusApplication(StatusTagSO tag, float durationOverride = -1f, int stacks = 1)
    {
      this.tag = tag;
      this.durationOverride = durationOverride;
      this.stacks = stacks;
    }
  }
}
