// File: SimplePool.cs
using UnityEngine;
using System.Collections.Generic;

public class SimplePool : MonoBehaviour
{
    public static SimplePool Instance { get; private set; }
    readonly Dictionary<GameObject, Queue<GameObject>> _pool = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!_pool.TryGetValue(prefab, out var q) || q.Count == 0)
            return Instantiate(prefab, pos, rot);

        var go = q.Dequeue();
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Recycle(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        if (!_pool.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            _pool[prefab] = q;
        }
        q.Enqueue(instance);
    }
}
