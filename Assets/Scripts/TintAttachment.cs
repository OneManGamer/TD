// File: TintAttachment.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime manager that blends tints from multiple status tokens using MaterialPropertyBlocks.
/// No shared-material edits.  Auto-clears when no tints remain.
/// </summary>
public class TintAttachment : MonoBehaviour
{
  private struct TintEntry
  {
    public Color color;
    public float weight;     // 0..1
    public bool pulse;
    public float pulseSpeed; // cycles per second
  }

  // Active tints keyed by a token.  Token can be an int, string, ScriptableObject, MonoBehaviour, etc.
  private readonly Dictionary<object, TintEntry> _active = new Dictionary<object, TintEntry>();

  private Renderer[] _renderers;
  private MaterialPropertyBlock _mpb;

  // Optional token generator for callers that do not want to manage their own keys
  private int _nextToken = 1;

  private void Awake()
  {
    _renderers = GetComponentsInChildren<Renderer>(true);
    _mpb = new MaterialPropertyBlock();
  }

  // ---------- Public API: add tints ----------

  /// <summary>
  /// Primary API.  Add or update a tint keyed by any object token.  Call RemoveTint with the same token to clear it.
  /// </summary>
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
    enabled = true;
  }

  /// <summary>
  /// Convenience overload for UnityEngine.Object keys like ScriptableObjects or MonoBehaviours.
  /// </summary>
  public void AddTint(Object key, Color color, float weight = 0.6f, bool pulse = false, float pulseSpeed = 4f)
  {
    AddTint((object)key, color, weight, pulse, pulseSpeed);
  }

  /// <summary>
  /// Convenience overload for string keys.
  /// </summary>
  public void AddTint(string key, Color color, float weight = 0.6f, bool pulse = false, float pulseSpeed = 4f)
  {
    AddTint((object)key, color, weight, pulse, pulseSpeed);
  }

  /// <summary>
  /// Token-generating overload.  Returns an int token you can pass to RemoveTint(int).
  /// </summary>
  public int AddTint(Color color, float weight = 0.6f, bool pulse = false, float pulseSpeed = 4f)
  {
    int token = _nextToken++;
    AddTint((object)token, color, weight, pulse, pulseSpeed);
    return token;
  }

  // ---------- Public API: remove tints ----------

  public void RemoveTint(object token)
  {
    if (token == null) return;
    if (_active.Remove(token))
    {
      if (_active.Count == 0)
      {
        // Clear all property blocks when no tints remain
        if (_renderers != null)
        {
          for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i]) _renderers[i].SetPropertyBlock(null);
        }
        enabled = false;
      }
    }
  }

  public void RemoveTint(Object key)   { RemoveTint((object)key); }
  public void RemoveTint(string key)   { RemoveTint((object)key); }
  public void RemoveTint(int token)    { RemoveTint((object)token); }

  // ---------- Runtime blending ----------

  private void Update()
  {
    if (_active.Count == 0) { enabled = false; return; }

    float tNow = Time.time;
    Color accum = Color.black;
    float accumW = 0f;

    // Blend all active tints.  Pulsing weights animate between 0.5x and 1.0x.
    foreach (var kv in _active)
    {
      var e = kv.Value;
      float w = e.weight;
      if (e.pulse && e.pulseSpeed > 0f)
      {
        float s = 0.5f + 0.5f * Mathf.Sin(tNow * Mathf.PI * 2f * e.pulseSpeed);
        w *= s;
      }
      accum += e.color * w;
      accumW += w;
    }

    // If multiple layers push total weight above 1, cap influence at 1.
    // Use the average tint color and a single effective strength in [0,1].
    Color avg = (accumW > 1e-4f) ? (accum / accumW) : Color.white;
    float effectiveStrength = Mathf.Clamp01(accumW);
    // Compose final by lerping from white to avg by effective strength.
    // This mimics your original approach while keeping brightness sane.
    Color final = Color.Lerp(Color.white, avg, effectiveStrength);
    final.a = 1f;

    if (_renderers != null)
    {
      for (int i = 0; i < _renderers.Length; i++)
      {
        var r = _renderers[i];
        if (!r) continue;

        _mpb.Clear();

        // Try URP Lit first, then Standard.  Setting both is safe for mixed materials.
        _mpb.SetColor("_BaseColor", final); // URP Lit
        _mpb.SetColor("_Color", final);     // Standard

        r.SetPropertyBlock(_mpb);
      }
    }
  }

  private void OnDisable()
  {
    if (_renderers != null)
    {
      for (int i = 0; i < _renderers.Length; i++)
        if (_renderers[i]) _renderers[i].SetPropertyBlock(null);
    }
    _active.Clear();
  }
}
