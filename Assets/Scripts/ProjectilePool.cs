using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    private readonly Dictionary<GameObject, Stack<GameObject>> _pools =
        new Dictionary<GameObject, Stack<GameObject>>();
    private Transform _root;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _root = new GameObject("ProjectilePool_Root").transform;
        _root.SetParent(transform, false);
        _root.gameObject.SetActive(true);
    }

    public T Spawn<T>(T prefab, Vector3 pos, Quaternion rot) where T : MonoBehaviour
    {
        if (!prefab) { Debug.LogError("ProjectilePool.Spawn: null prefab"); return null; }

        var key = prefab.gameObject;
        if (!_pools.TryGetValue(key, out var stack) || stack.Count == 0)
        {
            var goNew = Instantiate(key, pos, rot);
            Tag(goNew, key);
            NotifyOnSpawned(goNew);
            goNew.SetActive(true);
            return goNew.GetComponent<T>();
        }

        var go = stack.Pop();
        go.transform.SetPositionAndRotation(pos, rot);
        NotifyOnSpawned(go);
        go.SetActive(true);
        return go.GetComponent<T>();
    }

    public void Recycle(MonoBehaviour inst)
    {
        if (!inst) return;
        var go = inst.gameObject;
        var tag = go.GetComponent<PooledProjectile>();
        if (tag == null || tag.Prefab == null) { Destroy(go); return; }

        NotifyOnRecycled(go);
        go.SetActive(false);
        go.transform.SetParent(_root, false);
        _pools[tag.Prefab].Push(go);
    }

    public void Prewarm<T>(T prefab, int count) where T : MonoBehaviour
    {
        if (!prefab || count <= 0) return;
        var key = prefab.gameObject;
        if (!_pools.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>(count);
            _pools[key] = stack;
        }

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(key);
            Tag(go, key);
            NotifyOnRecycled(go); // give components a chance to clear
            go.SetActive(false);
            go.transform.SetParent(_root, false);
            stack.Push(go);
        }
    }

    // --- helpers ---
    void Tag(GameObject go, GameObject prefab)
    {
        var t = go.GetComponent<PooledProjectile>();
        if (!t) t = go.AddComponent<PooledProjectile>();
        t.Prefab = prefab;
        t.Pool = this;
    }

    static void NotifyOnSpawned(GameObject go)
    {
        var poolables = go.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();
    }

    static void NotifyOnRecycled(GameObject go)
    {
        var poolables = go.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++) poolables[i].OnRecycled();
    }
}
