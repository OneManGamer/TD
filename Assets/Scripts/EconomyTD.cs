using UnityEngine;

public class EconomyTD : MonoBehaviour {
    public static EconomyTD Instance { get; private set; }
    void Awake() { Instance = this; }
    public int gold = 120;

    public bool TrySpend(int amount) {
        if (gold < amount) return false;
        gold -= amount;
        UIHUD_TD.Instance.Refresh();
        return true;
    }
    public void AddGold(int amount) { gold += amount; UIHUD_TD.Instance.Refresh(); }
}
