// File: TintAttachment.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime manager that blends tints from multiple status tokens using MaterialPropertyBlocks.
/// No shared-material edits; auto-clears when no tints remain.
/// </summary>
public class TintAttachment : MonoBehaviour
{
    private struct TintEntry
    {
        public Color color;
        public float weight;     // 0..1
        public bool pulse;
        public float pulseSpeed; // Hz-ish (cycles per second)
    }

    private readonly Dictionary<object, TintEntry> _active = new Dictionary<object, TintEntry>();
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private bool _dirty; // re-apply this frame

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
    }

    public void AddTint(object token, Color color, float weight = 0.6f, bool pulse = false, float pulseSpeed = 4f)
    {
        if (token == null) return;
        _active[token] = new TintEntry
        {
            color = color,
            weight = Mathf.Clamp01(weight),
            pulse = pulse,
            pulseSpeed = Mathf.Max(0f, pulseSpeed)
        };
        _dirty = true;
        enabled = true;
    }

    public void RemoveTint(object token)
    {
        if (token == null) return;
        if (_active.Remove(token))
        {
            _dirty = true;
            if (_active.Count == 0)
            {
                // Clear all property blocks
                if (_renderers != null)
                {
                    for (int i = 0; i < _renderers.Length; i++)
                        if (_renderers[i]) _renderers[i].SetPropertyBlock(null);
                }
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (_active.Count == 0) { enabled = false; return; }

        // Blend all active tints
        float t = Time.time;
        Color accum = Color.black;
        float accumW = 0f;

        foreach (var kv in _active)
        {
            var e = kv.Value;
            float w = e.weight;
            if (e.pulse && e.pulseSpeed > 0f)
            {
                // 0.5..1.0 pulsing weight
                float s = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * e.pulseSpeed);
                w *= s;
            }
            accum += e.color * w;
            accumW += w;
        }

        Color final = (accumW > 1e-4f) ? (accum / Mathf.Min(1f, accumW)) : Color.white;
        final.a = 1f;

        // Apply via MPB. Try URP Lit (_BaseColor) first, then Standard (_Color).
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (!r) continue;
                _mpb.Clear();
                _mpb.SetColor("_BaseColor", final); // URP Lit
                _mpb.SetColor("_Color", final);     // Standard
                r.SetPropertyBlock(_mpb);
            }
        }

        _dirty = false;
    }

    void OnDisable()
    {
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i]) _renderers[i].SetPropertyBlock(null);
        }
        _active.Clear();
    }
}
