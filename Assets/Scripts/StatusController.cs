using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Holds active status tags with durations and stacks.  Attach to enemies.
  /// </summary>
  public class StatusController : MonoBehaviour
  {
    [Serializable]
    public class ActiveStatus
    {
      public StatusTagSO tag;
      public float remaining;
      public int stacks;

      public ActiveStatus(StatusTagSO tag, float duration, int stacks)
      {
        this.tag = tag;
        this.remaining = duration;
        this.stacks = stacks;
      }
    }

    private readonly List<ActiveStatus> _statuses = new List<ActiveStatus>();

    public bool HasTag(StatusTagSO tag)
    {
      if (tag == null) return false;
      for (int i = 0; i < _statuses.Count; i++)
        if (_statuses[i].tag == tag) return true;
      return false;
    }

    public int GetStacks(StatusTagSO tag)
    {
      if (tag == null) return 0;
      for (int i = 0; i < _statuses.Count; i++)
        if (_statuses[i].tag == tag) return _statuses[i].stacks;
      return 0;
    }

    public void ConsumeTag(StatusTagSO tag)
    {
      if (tag == null) return;
      for (int i = 0; i < _statuses.Count; i++)
      {
        if (_statuses[i].tag == tag)
        {
          _statuses.RemoveAt(i);
          return;
        }
      }
    }

    public void Apply(StatusApplication app)
    {
      if (app.tag == null) return;

      float dur = app.durationOverride > 0f ? app.durationOverride : Mathf.Max(0.01f, app.tag.DefaultDuration);
      int stacks = Mathf.Max(1, app.stacks);

      // Exclusivity handling.
      if (!string.IsNullOrEmpty(app.tag.ExclusiveGroup))
      {
        for (int i = _statuses.Count - 1; i >= 0; i--)
        {
          var s = _statuses[i];
          if (s.tag != null && s.tag != app.tag && s.tag.ExclusiveGroup == app.tag.ExclusiveGroup)
          {
            _statuses.RemoveAt(i);
          }
        }
      }

      // Update existing or add new.
      for (int i = 0; i < _statuses.Count; i++)
      {
        if (_statuses[i].tag == app.tag)
        {
          if (app.tag.Stackable)
          {
            _statuses[i].stacks = Mathf.Clamp(_statuses[i].stacks + stacks, 1, Mathf.Max(1, app.tag.MaxStacks));
            _statuses[i].remaining = Mathf.Max(_statuses[i].remaining, dur);
          }
          else
          {
            _statuses[i].stacks = 1;
            _statuses[i].remaining = dur;
          }
          return;
        }
      }

      _statuses.Add(new ActiveStatus(app.tag, dur, app.tag.Stackable ? Mathf.Min(stacks, Mathf.Max(1, app.tag.MaxStacks)) : 1));
    }

    public void ApplyMany(IList<StatusApplication> apps)
    {
      if (apps == null) return;
      for (int i = 0; i < apps.Count; i++) Apply(apps[i]);
    }

    private void Update()
    {
      float dt = Time.deltaTime;
      for (int i = _statuses.Count - 1; i >= 0; i--)
      {
        _statuses[i].remaining -= dt;
        if (_statuses[i].remaining <= 0f)
        {
          _statuses.RemoveAt(i);
        }
      }
    }
  }
}
