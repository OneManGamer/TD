using UnityEngine;

public class UIHUD_TD : MonoBehaviour {
    public static UIHUD_TD Instance { get; private set; }
    void Awake() { Instance = this; }
    public void Refresh() {}

    void OnGUI() {
        GUI.Label(new Rect(10, 10, 220, 24), $"Gold: {EconomyTD.Instance.gold}");
        GUI.Label(new Rect(10, 34, 220, 24), $"Base HP: {LevelController.Instance.baseHP}");
        GUI.Label(new Rect(10, 58, 420, 24), "Left click to place an Arrow Tower.");
    }
}
