using UnityEngine;

namespace TD.Combat
{
  public class AddTestTagOnKey : MonoBehaviour
  {
    public StatusController controller;
    public StatusTagSO statusTag;      // renamed from 'tag' to avoid hiding Component.tag
    public int stacks = 1;
    public float durationOverride = -1f; // -1 = use StatusTagSO.DefaultDuration

    void Update()
    {
      if (Input.GetKeyDown(KeyCode.Alpha4) && controller && statusTag)
      {
        controller.Add(statusTag, durationOverride, stacks);
        Debug.Log($"[Tags] Added {statusTag.name} x{stacks} (durOverride={durationOverride})");
      }
    }
  }
}
