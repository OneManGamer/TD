using UnityEngine;

public class GridDebugClick : MonoBehaviour {
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            var g = GridService.Instance;
            var cam = Camera.main;
            if (cam != null && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 200f)) {
                var c = g.WorldToCell(hit.point);
                Debug.Log($"Cell under mouse: {c}");
            }
        }
    }
}
