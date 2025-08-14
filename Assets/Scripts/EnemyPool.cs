using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Tooltip("Optional prewarm count used when you call Prewarm(prefab, count).")]
    [SerializeField] private int defaultPrewarm = 8;

    private readonly Dictionary<EnemyAgentFlow, Stack<EnemyAgentFlow>> _pools = new Dictionary<EnemyAgentFlow, Stack<EnemyAgentFlow>>();
    private Transform _root;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Keep pooled objects tidy in hierarchy.
        _root = new GameObject("EnemyPool_Root").transform;
        _root.SetParent(transform, false);
        _root.gameObject.SetActive(true);
    }

    /// <summary>Optional. Pre-create 'count' enemies of this prefab into the pool.</summary>
    public void Prewarm(EnemyAgentFlow prefab, int count = -1)
    {
        if (count < 0) count = defaultPrewarm;
        if (count <= 0 || prefab == null) return;

        if (!_pools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<EnemyAgentFlow>(count);
            _pools[prefab] = stack;
        }

        for (int i = 0; i < count; i++)
        {
            var inst = CreateNew(prefab);
            PrepareForPool(inst);
            stack.Push(inst);
        }
    }

    /// <summary>Spawn from pool or create if empty.</summary>
    public EnemyAgentFlow Spawn(EnemyAgentFlow prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null)
        {
            Debug.LogError("EnemyPool.Spawn called with null prefab.");
            return null;
        }

        if (!_pools.TryGetValue(prefab, out var stack) || stack.Count == 0)
        {
            var created = CreateNew(prefab);
            Activate(created, pos, rot);
            return created;
        }

        var inst = stack.Pop();
        Activate(inst, pos, rot);
        return inst;
    }

    /// <summary>Recycle an enemy back into its prefab pool. Falls back to Destroy if not pool-managed.</summary>
    public void Recycle(EnemyAgentFlow inst)
    {
        if (inst == null) return;

        var tag = inst.GetComponent<PooledEnemy>();
        if (tag == null || tag.Prefab == null)
        {
            // Was not created by this pool. Fall back to Destroy so we do not leak.
            Destroy(inst.gameObject);
            return;
        }

        // Notify ALL IPoolable components before deactivation.
        var poolables = inst.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnRecycled();

        PrepareForPool(inst);

        if (!_pools.TryGetValue(tag.Prefab, out var stack))
        {
            stack = new Stack<EnemyAgentFlow>();
            _pools[tag.Prefab] = stack;
        }
        stack.Push(inst);
    }

    // Convenience static wrappers
    public static EnemyAgentFlow SpawnStatic(EnemyAgentFlow prefab, Vector3 pos, Quaternion rot) => Instance.Spawn(prefab, pos, rot);
    public static void RecycleStatic(EnemyAgentFlow inst) => Instance.Recycle(inst);

    // Internal helpers
    private EnemyAgentFlow CreateNew(EnemyAgentFlow prefab)
    {
        var inst = Instantiate(prefab);
        var tag = inst.gameObject.GetComponent<PooledEnemy>();
        if (tag == null) tag = inst.gameObject.AddComponent<PooledEnemy>();
        tag.Prefab = prefab;
        tag.Pool = this;
        return inst;
    }

    private void Activate(EnemyAgentFlow inst, Vector3 pos, Quaternion rot)
    {
        inst.transform.SetParent(null, false);
        inst.transform.SetPositionAndRotation(pos, rot);

        // Notify ALL IPoolable components BEFORE activation so OnEnable sees reset state (e.g., Health).
        var poolables = inst.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnSpawned();

        inst.gameObject.SetActive(true);
    }

    private void PrepareForPool(EnemyAgentFlow inst)
    {
        inst.gameObject.SetActive(false);
        inst.transform.SetParent(_root, false);
    }
}
