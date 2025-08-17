using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TD.Combat
{
  public class ActiveTagsHUD : MonoBehaviour
  {
    public StatusController controller;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);
    public GUIStyle style;

    void Reset() => controller = GetComponentInParent<StatusController>();

    void OnGUI()
    {
      if (!controller) return;

      FieldInfo fi = typeof(StatusController).GetField("_statuses",
        BindingFlags.NonPublic | BindingFlags.Instance);

      var list = fi != null
        ? fi.GetValue(controller) as List<StatusController.ActiveStatus>
        : null;

      if (list == null || list.Count == 0) return;

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < list.Count; i++)
      {
        var s = list[i];
        var tag = s.tag;
        if (!tag) continue;

        string name = tag.name;
        string group = tag.ExclusiveGroup;
        string pretty = name;

        if (name == "FreezeImmune" || tag.TagId == "FreezeImmune")
  pretty = $"<b><color=#80E0FF>FreezeImmune</color></b>";
else if (group == "ColdTemp")
  pretty = $"<color=#66CCFF>{name}</color>";   // cool blue
else if (group == "Heat")
  pretty = $"<color=#FF9933>{name}</color>";   // warm orange

        sb.AppendLine($"{pretty}  t={s.remaining:0.0}s  x{s.stacks}");
      }

      var cam = Camera.main;
      if (!cam) return;

      Vector3 screen = cam.WorldToScreenPoint(transform.position + worldOffset);
      if (screen.z < 0) return;

      if (style == null)
      {
        style = new GUIStyle(GUI.skin.label)
        {
          fontSize = 14,
          richText = true,
          normal = { textColor = Color.white }
        };
      }

      var content = new GUIContent(sb.ToString());
      Vector2 size = style.CalcSize(content);
      Rect r = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y, size.x, size.y);
      GUI.Label(r, content, style);
    }
  }
}
