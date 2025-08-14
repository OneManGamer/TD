// File: BountyOnDeath.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class BountyOnDeath : MonoBehaviour
{
    public EnemyDefinitionSO definition;

    Health _h;

    void Awake()
    {
        _h = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (_h != null) _h.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (_h != null) _h.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        if (definition && EconomyTD.Instance) EconomyTD.Instance.AddGold(definition.bountyGold);
    }
}
