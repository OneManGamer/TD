// File: EconomyTD.cs
using UnityEngine;
using System;

public class EconomyTD : MonoBehaviour
{
    public static EconomyTD Instance { get; private set; }

    [Header("Currency")]
    public int gold = 120;
    public event Action<int> OnGoldChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (gold < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }
}
