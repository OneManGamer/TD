using UnityEngine;

[RequireComponent(typeof(Health))]
public class RecycleOnDeath : MonoBehaviour
{
    Health _health;
    EnemyAgentFlow _enemy;

    void Awake()
    {
        _health = GetComponent<Health>();
        _enemy = GetComponent<EnemyAgentFlow>(); // optional, will re-get if null
    }

    void OnEnable()
    {
        if (_health != null) _health.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        // Health will call SetActive(false) after invoking OnDeath.  We recycle first.
        if (_enemy == null) _enemy = GetComponent<EnemyAgentFlow>();

        if (EnemyPool.Instance != null && _enemy != null)
        {
            EnemyPool.Instance.Recycle(_enemy);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
