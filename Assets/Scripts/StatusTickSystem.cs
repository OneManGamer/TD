using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TD.Combat;

/// <summary>
/// Applies periodic damage based on active status tags without assuming exact method names on StatusController.
/// Uses reflection to support variants like Has/Contains and GetStacks/GetCount.
/// </summary>
[RequireComponent(typeof(StatusController))]
public class StatusTickSystem : MonoBehaviour
{
    [Serializable]
    public class TagDps
    {
        public StatusTagSO tag;
        public float dps = 0f;
        public bool scaleByStacks = true;
        public DamageType damageType = DamageType.Physical;
    }

    [SerializeField] private List<TagDps> entries = new List<TagDps>(4);

    StatusController _status;
    IDamageable _damageable;

    // cached reflection
    static MethodInfo _miHas;
    static MethodInfo _miContains;
    static MethodInfo _miGetStacks;
    static MethodInfo _miGetCount;

    void Awake()
    {
        _status = GetComponent<StatusController>();
        _damageable = GetComponentInParent<IDamageable>();

        var t = typeof(StatusController);
        _miHas       = t.GetMethod("Has",       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(StatusTagSO) }, null);
        _miContains  = t.GetMethod("Contains",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(StatusTagSO) }, null);
        _miGetStacks = t.GetMethod("GetStacks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(StatusTagSO) }, null);
        _miGetCount  = t.GetMethod("GetCount",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(StatusTagSO) }, null);
    }

    void Update()
    {
        if (_status == null || _damageable == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float total = 0f;
        DamageType lastType = DamageType.Physical; // used only if we apply a single blended tick

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.tag == null) continue;

            int stacks = GetStacksSafe(_status, e.tag);
            bool present = stacks > 0 || HasSafe(_status, e.tag);
            if (!present) continue;

            int mult = e.scaleByStacks ? Math.Max(1, stacks) : 1;
            total += e.dps * mult * dt;
            lastType = e.damageType; // note: if multiple types are present, we just use the last one for the blended tick
        }

        if (total > 0f)
        {
            var ctx = new HitContext(gameObject, gameObject, lastType, transform.position, Vector3.up);
            CombatIntegrationAPI.ApplyHit(gameObject, gameObject, total, lastType, in ctx);
        }
    }

    static bool HasSafe(StatusController sc, StatusTagSO tag)
    {
        if (sc == null || tag == null) return false;
        if (_miHas != null)      return (bool)_miHas.Invoke(sc, new object[] { tag });
        if (_miContains != null) return (bool)_miContains.Invoke(sc, new object[] { tag });
        // If there is a stacks method, treat stacks > 0 as present
        int stacks = GetStacksSafe(sc, tag);
        return stacks > 0;
    }

    static int GetStacksSafe(StatusController sc, StatusTagSO tag)
    {
        if (sc == null || tag == null) return 0;
        if (_miGetStacks != null) return (int)_miGetStacks.Invoke(sc, new object[] { tag });
        if (_miGetCount  != null) return (int)_miGetCount.Invoke(sc, new object[] { tag });
        return 0;
    }
}
