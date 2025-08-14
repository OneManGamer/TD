using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    public GameObject Prefab;
    public ProjectilePool Pool;
}

// Reuse the same IPoolable interface you already added for enemies:
// public interface IPoolable { void OnSpawned(); void OnRecycled(); }
