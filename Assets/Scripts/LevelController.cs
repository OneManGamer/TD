using UnityEngine;

public class LevelController : MonoBehaviour {
    public static LevelController Instance { get; private set; }
    void Awake() { Instance = this; }

    public int baseHP = 10;

    public void OnEnemyReachedGoal() {
        baseHP -= 1;
        UIHUD_TD.Instance.Refresh();
        if (baseHP <= 0) Debug.Log("Defeat.  Reload or return to hub.");
    }

    public void OnEnemyKilled(int gold) {
        EconomyTD.Instance.AddGold(gold);
    }
}
