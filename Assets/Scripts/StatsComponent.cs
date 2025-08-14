// File: StatsComponent.cs
using UnityEngine;

/// <summary>
/// Attach to any entity (enemy or tower) to hold a StatBlock + convenient inspector bases.
/// Only MoveSpeed and DamageTaken are seeded here for now; extend as needed.
/// </summary>
public class StatsComponent : MonoBehaviour
{
    [Header("Base Stats")]
    [Tooltip("Base movement speed (enemies).")]
    public float baseMoveSpeed = 2.2f;

    [Tooltip("1.0 = normal damage; >1 takes more; <1 takes less.")]
    public float baseDamageTaken = 1.0f;

    [SerializeField] private StatBlock _block = new StatBlock();
    public StatBlock Block => _block;

    void Awake()  { SeedBaseValues(); }
    void OnValidate() { SeedBaseValues(); }

    private void SeedBaseValues()
    {
        if (_block == null) return;
        _block.SetBase(StatIDs.MoveSpeed,   Mathf.Max(0f, baseMoveSpeed));
        _block.SetBase(StatIDs.DamageTaken, Mathf.Max(0f, baseDamageTaken));
    }

    /// <summary>Helper to read a stat with a default fallback.</summary>
    public float GetFinal(string statId, float defaultBase) =>
        _block != null ? _block.GetFinal(statId, defaultBase) : defaultBase;
}
