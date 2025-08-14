// File: StatBlock.cs
using System; // <-- needed for Comparison<T>
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds base values and runtime modifiers for arbitrary stats by string ID.
/// Evaluate with GetFinal(statId, defaultBase).
/// </summary>
[System.Serializable]
public class StatBlock
{
    [SerializeField] private Dictionary<string, float> _base = new Dictionary<string, float>();
    [System.NonSerialized] private Dictionary<string, List<StatModifier>> _mods = new Dictionary<string, List<StatModifier>>();

    // temp compare to avoid allocs during sort
    private static readonly Comparison<StatModifier> _cmp = (a, b) =>
    {
        int o = a.order.CompareTo(b.order);
        if (o != 0) return o;
        // Stable op order within same "order" value: Add -> Mul -> Override
        return ((int)a.op).CompareTo((int)b.op);
    };

    public void SetBase(string statId, float value)
    {
        if (string.IsNullOrEmpty(statId)) return;
        _base[statId] = value;
    }

    public bool TryGetBase(string statId, out float value)
    {
        return _base.TryGetValue(statId, out value);
    }

    /// <summary>Returns the final (base + modifiers) value. If no base exists, uses defaultBase.</summary>
    public float GetFinal(string statId, float defaultBase = 0f)
    {
        if (string.IsNullOrEmpty(statId)) return defaultBase;

        float result = defaultBase;
        if (_base.TryGetValue(statId, out var b)) result = b;

        if (_mods != null && _mods.TryGetValue(statId, out var list) && list != null && list.Count > 0)
        {
            list.Sort(_cmp);
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                switch (m.op)
                {
                    case StatOp.Add:      result += m.value; break;
                    case StatOp.Mul:      result *= m.value; break;
                    case StatOp.Override: result  = m.value; break;
                }
            }
        }
        return result;
    }

    public void AddModifier(StatModifier mod)
    {
        if (mod == null || string.IsNullOrEmpty(mod.statId)) return;
        if (_mods == null) _mods = new Dictionary<string, List<StatModifier>>();
        if (!_mods.TryGetValue(mod.statId, out var list))
        {
            list = new List<StatModifier>(4);
            _mods[mod.statId] = list;
        }
        list.Add(mod);
    }

    /// <summary>Removes all modifiers that share the same sourceToken (used by StatusRuntime).</summary>
    public void RemoveBySource(object sourceToken)
    {
        if (sourceToken == null || _mods == null) return;
        foreach (var kv in _mods)
        {
            var list = kv.Value;
            if (list == null || list.Count == 0) continue;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(list[i].sourceToken, sourceToken))
                    list.RemoveAt(i);
            }
        }
    }

    public void ClearAll()
    {
        _base.Clear();
        _mods?.Clear();
    }
}
