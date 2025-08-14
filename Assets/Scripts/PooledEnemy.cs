using UnityEngine;

/// <summary>Attached automatically to pooled enemies.  Tracks their original prefab and owning pool.</summary>
public class PooledEnemy : MonoBehaviour
{
    public EnemyAgentFlow Prefab;
    public EnemyPool Pool;
}

/// <summary>Optional hook for enemies that want to reset state on spawn/recycle.</summary>
public interface IPoolable
{
    void OnSpawned();
    void OnRecycled();
}
