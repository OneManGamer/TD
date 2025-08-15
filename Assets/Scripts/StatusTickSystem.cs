using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Reads active tags and applies periodic damage like burn or poison.
  /// One per enemy.
  /// </summary>
  [RequireComponent(typeof(StatusController))]
  public class StatusTickSystem : MonoBehaviour
  {
    [System.Serializable]
    public class TagDps
    {
      public StatusTagSO tag;
      public float dps = 0f;
      public bool scaleByStacks = true;
    }

    [Header("Damage over time mapping")]
    [SerializeField] private List<TagDps> dotMap = new List<TagDps>();

    private StatusController _status;
    private IDamageable _damageable;

    private void Awake()
    {
      _status = GetComponent<StatusController>();
      _damageable = GetComponent<IDamageable>();
    }

    private void Update()
    {
      if (_status == null || _damageable == null) return;

      float dt = Time.deltaTime;
      float total = 0f;

      for (int i = 0; i < dotMap.Count; i++)
      {
        var e = dotMap[i];
        if (e.tag == null || e.dps <= 0f) continue;

        if (_status.HasTag(e.tag))
        {
          int stacks = e.scaleByStacks ? Mathf.Max(1, _status.GetStacks(e.tag)) : 1;
          total += e.dps * stacks * dt;
        }
      }

      if (total > 0f)
      {
        var ctx = new HitContext(gameObject, gameObject, DamageType.Physical, total);
        _damageable.ApplyDamage(total, ctx);
      }
    }
  }
}
