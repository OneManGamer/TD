using UnityEngine;

public class PlacementGrid : MonoBehaviour {
    public Tower towerPrefab;
    public int towerCost = 60;
    public LayerMask groundMask;
    public float gridSize = 1f;

    void Update() {
        if (Input.GetMouseButtonDown(0)) TryPlace();
    }

    void TryPlace() {
    if (towerPrefab == null) { Debug.LogWarning("No towerPrefab assigned."); return; }

    Camera cam = Camera.main;
#if UNITY_2023_1_OR_NEWER
    if (cam == null) cam = FindFirstObjectByType<Camera>();
#else
    if (cam == null) cam = FindObjectOfType<Camera>();
#endif
    if (cam == null) { Debug.LogError("No Camera found."); return; }

    if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 200f, groundMask)) return;

    var grid = GridService.Instance;
    Vector2Int cell = grid.WorldToCell(hit.point);

    if (!grid.InBounds(cell)) { Debug.Log("Out of bounds."); return; }

    // NEW: respect static build rules
    if (!grid.CanBuildBase(cell)) { Debug.Log("You cannot build here."); return; }

    // Already occupied by a placed tower?
    if (grid.IsBlocked(cell)) { Debug.Log("Cell occupied."); return; }

    // NEW: validate walk reachability if this placement blocks the cell
    if (!grid.AllSpawnsReachGoalIf(cell, true)) {
        Debug.Log("You must leave at least one route to the goal.");
        return;
    }

    if (!EconomyTD.Instance.TrySpend(towerCost)) { Debug.Log("Not enough gold."); return; }

    grid.SetBlocked(cell, true);
    grid.RebuildFlow();

    Vector3 placePos = grid.CellCenter(cell);
    var t = Instantiate(towerPrefab, placePos, Quaternion.identity);
    t.tag = "Tower";
}
}
