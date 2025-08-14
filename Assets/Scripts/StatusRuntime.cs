// File: StatusRuntime.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
public class StatusRuntime : MonoBehaviour
{
    private class ActiveEffect
    {
        public StatusEffectSO effect;
        public string key;
        public object token;
        public float endTime;
    }

    private readonly List<ActiveEffect> _active = new List<ActiveEffect>(8);
    private readonly Dictionary<string, ActiveEffect> _byKey = new Dictionary<string, ActiveEffect>(8);
    private StatsComponent _stats;

    void Awake()
    {
        _stats = GetComponent<StatsComponent>();
    }

    /// <summary>
    /// Apply a status. If another effect with the same stacking key exists, refresh its timer (no stacking).
    /// Optional overrideDuration uses that time instead of effect.duration.
    /// Returns a handle token.
    /// </summary>
    public object Apply(StatusEffectSO effect, GameObject source = null, float? overrideDuration = null)
    {
        if (!effect || !_stats) return null;

        string key = effect.GetStackingKey();
        float dur = overrideDuration.HasValue ? Mathf.Max(0f, overrideDuration.Value) : effect.duration;

        // Already have an effect with this key? Refresh its timer, do NOT add modifiers/tints again.
        if (_byKey.TryGetValue(key, out var existing))
        {
            existing.endTime = Time.time + dur;
            return existing.token;
        }

        // New application
        var token = new object();

        // Add modifiers (tag with token so we can remove cleanly)
        var mods = effect.BuildModifiers(_stats, source);
        if (mods != null)
        {
            foreach (var m in mods)
            {
                if (m == null) continue;
                m.sourceToken = token;
                _stats.Block.AddModifier(m);
            }
        }

        // Visual / side-effect hook
        effect.OnApplied(gameObject, token);

        var ae = new ActiveEffect
        {
            effect = effect,
            key = key,
            token = token,
            endTime = Time.time + dur
        };
        _active.Add(ae);
        _byKey[key] = ae;

        return token;
    }

    /// <summary>Manually clear an effect by token.</summary>
    public void Remove(object token)
    {
        if (token == null) return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ae = _active[i];
            if (!ReferenceEquals(ae.token, token)) continue;

            _stats?.Block.RemoveBySource(token);
            ae.effect?.OnRemoved(gameObject, token);

            _byKey.Remove(ae.key);
            _active.RemoveAt(i);
            return;
        }
    }

    void Update()
    {
        if (_active.Count == 0) return;

        float now = Time.time;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ae = _active[i];
            if (now < ae.endTime) continue;

            _stats?.Block.RemoveBySource(ae.token);
            ae.effect?.OnRemoved(gameObject, ae.token);

            _byKey.Remove(ae.key);
            _active.RemoveAt(i);
        }
    }

    void OnDisable()
    {
        // Clear all active status contributions and visuals when pooled/disabled
        for (int i = 0; i < _active.Count; i++)
        {
            var ae = _active[i];
            _stats?.Block.RemoveBySource(ae.token);
            ae.effect?.OnRemoved(gameObject, ae.token);
        }
        _active.Clear();
        _byKey.Clear();
    }
}
