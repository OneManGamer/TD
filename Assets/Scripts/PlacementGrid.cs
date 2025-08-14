// File: PlacementGrid.cs
using UnityEngine;

public class PlacementGrid : MonoBehaviour
{
    [Header("Prefab")]
    public TowerShooter towerPrefab;

    [Header("Placement")]
    public LayerMask groundMask;
    public float clickMaxDistance = 400f;

    [Header("Cost")]
    public int towerCostOverride = -1;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryPlace();
    }

    void TryPlace()
    {
        var grid = GridService.Instance;
        if (grid == null) { Debug.LogError("No GridService found in scene."); return; }
        if (towerPrefab == null) { Debug.LogWarning("No towerPrefab assigned."); return; }

        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("No main camera."); return; }

        int mask = groundMask.value == 0 ? ~0 : groundMask.value;
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, clickMaxDistance, mask, QueryTriggerInteraction.Ignore)) return;

        Vector2Int cell = grid.WorldToCell(hit.point);
        if (!grid.InBounds(cell)) { Debug.Log("Clicked outside the grid."); return; }
        if (!grid.CanBuild(cell)) { Debug.Log("You cannot build on that cell."); return; }
        if (grid.IsBlocked(cell)) { Debug.Log("That cell is already occupied."); return; }

        bool wasBlocked = grid.IsBlocked(cell);
        grid.SetBlocked(cell, true);
        grid.RebuildFlow();

        bool routeOk = true;
        foreach (var s in grid.spawnCells)
        {
            if (!grid.InBounds(s)) continue;
            if (grid.Distance(s) == int.MaxValue) { routeOk = false; break; }
        }

        if (!routeOk)
        {
            grid.SetBlocked(cell, wasBlocked);
            grid.RebuildFlow();
            Debug.Log("You must leave at least one route to the goal.");
            return;
        }

        int cost = towerCostOverride >= 0 ? towerCostOverride :
                   (towerPrefab.definition ? towerPrefab.definition.buildCost : 60);

        if (!EconomyTD.Instance || !EconomyTD.Instance.TrySpend(cost))
        {
            grid.SetBlocked(cell, wasBlocked);
            grid.RebuildFlow();
            Debug.Log("Not enough gold.");
            return;
        }

        Vector3 placePos = grid.CellCenter(cell);
        var t = Instantiate(towerPrefab, placePos, Quaternion.identity);
        t.tag = "Tower";
    }
}
