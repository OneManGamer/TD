using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Applies periodic damage (DPS) while specific StatusTagSO's are present on a StatusController.
  /// - Each entry has its own tick interval.
  /// - Supports per-stack scaling with diminishing returns.
  /// - Uses CombatIntegrationAPI so resistances, etc. still apply.
  /// Attach this near/with the target's StatusController.
  /// </summary>
  public class StatusTickSystem : MonoBehaviour
  {
    public enum StackScalingMode
    {
      None,             // no extra damage from stacks
      Linear,           // multiplier = stacks (original simple mode)
      Diminishing       // 1 + sum( baseBonusPerStack * diminishing^(k-1) ) for each extra stack
    }

    [Serializable]
    public class TickEntry
    {
      public StatusTagSO tag;

      [Tooltip("Damage per second while this tag is present (pre-resistance).")]
      public float dps = 4f;

      [Tooltip("Seconds between ticks. Damage per tick = dps * tickInterval * stackMultiplier.")]
      public float tickInterval = 0.5f;

      [Tooltip("Damage type applied for these ticks.")]
      public DamageType damageType = DamageType.Fire;

      [Header("Stack Scaling")]
      public StackScalingMode scaling = StackScalingMode.Diminishing;

      [Tooltip("For Linear: multiplier = stacks. For Diminishing: extra bonus % per stack (as 0.25 = +25% each, before diminishing).")]
      [Range(0f, 5f)] public float baseBonusPerStack = 0.25f; // used in Diminishing

      [Tooltip("Diminishing factor applied to each subsequent stack's bonus. 1 = no diminishing; 0.5 halves each additional stack's bonus.")]
      [Range(0f, 1f)] public float diminishing = 0.8f;

      [Tooltip("Clamp final stack multiplier to avoid runaway values. 0 or less = no clamp.")]
      public float maxStackMultiplier = 0f; // 0 = unlimited
    }

    [Tooltip("Define which tags cause periodic damage and how they tick.")]
    public List<TickEntry> entries = new List<TickEntry>();

    [Tooltip("Optional: log every tick to the Console.")]
    public bool debugLogs = false;

    StatusController _controller;
    IDamageable _damageable;
    GameObject _targetGO;

    float[] _accum;

    void Awake()
    {
      _controller = GetComponentInParent<StatusController>();
      _damageable = GetComponentInParent<IDamageable>();
      _targetGO = _damageable != null ? ((MonoBehaviour)_damageable).gameObject : gameObject;

      if (_controller == null)
        Debug.LogWarning("[StatusTickSystem] No StatusController found in parent chain. Ticks will not apply.");
      if (_damageable == null)
        Debug.LogWarning("[StatusTickSystem] No IDamageable found in parent chain. Ticks will not apply.");

      ResizeAccum();
    }

    void OnValidate()
    {
      if (_accum == null || (entries != null && _accum.Length != entries.Count))
        ResizeAccum();
    }

    void ResizeAccum()
    {
      int n = entries != null ? entries.Count : 0;
      _accum = new float[n];
    }

    void Update()
    {
      if (_controller == null || _damageable == null || entries == null || entries.Count == 0) return;
      if (_accum == null || _accum.Length != entries.Count) ResizeAccum();

      float dt = Time.deltaTime;

      for (int i = 0; i < entries.Count; i++)
      {
        var e = entries[i];
        if (e == null || e.tag == null || e.tickInterval <= 0f || e.dps <= 0f) continue;

        if (!_controller.Has(e.tag))
        {
          _accum[i] = 0f;
          continue;
        }

        _accum[i] += dt;

        while (_accum[i] >= e.tickInterval)
        {
          _accum[i] -= e.tickInterval;

          int stacks = Mathf.Max(1, _controller.GetStacks(e.tag));
          float stackMult = ComputeStackMultiplier(e, stacks);

          float dmg = e.dps * e.tickInterval * stackMult;

          var ctx = new HitContext(
            source: gameObject,
            target: _targetGO,
            damageType: e.damageType,
            hitPoint: _targetGO.transform.position,
            hitNormal: Vector3.up,
            isCrit: false,
            critMultiplier: 1f,
            userData: "StatusTick"
          );

          CombatIntegrationAPI.ApplyHit(gameObject, _targetGO, dmg, e.damageType, in ctx);

          if (debugLogs)
          {
            Debug.Log($"[StatusTick] {e.tag.name} stacks={stacks} mult={stackMult:0.###} dmg={dmg:0.###} type={e.damageType}");
          }
        }
      }
    }

    static float ComputeStackMultiplier(TickEntry e, int stacks)
    {
      if (stacks <= 0) return 0f;

      switch (e.scaling)
      {
        case StackScalingMode.None:
          return 1f;

        case StackScalingMode.Linear:
          return stacks;

        case StackScalingMode.Diminishing:
          // Baseline 1x at 1 stack. Each additional stack adds a decayed bonus.
          int extra = Mathf.Max(0, stacks - 1);
          float bonus = 0f;
          float add = Mathf.Max(0f, e.baseBonusPerStack);
          float decay = Mathf.Clamp01(e.diminishing);
          for (int i = 0; i < extra; i++)
          {
            bonus += add;
            add *= decay;
          }
          float mult = 1f + bonus;
          if (e.maxStackMultiplier > 0f) mult = Mathf.Min(mult, e.maxStackMultiplier);
          return mult;
      }
      return 1f;
    }
  }
}
