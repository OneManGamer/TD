using UnityEngine;

public class GridArea : MonoBehaviour {
    public enum AreaRule { Both, WalkOnly, BuildOnly, Forbidden }

    [Header("Cell Rect (grid coordinates)")]
    public Vector2Int cellMin = new Vector2Int(0, 0);  // bottom-left cell
    public Vector2Int size    = new Vector2Int(5, 5);  // width/height in cells

    [Header("Rule")]
    public AreaRule rule = AreaRule.WalkOnly;

    [Header("Priority (higher wins on overlap)")]
    public int priority = 0;

    public RectInt CellRect => new RectInt(cellMin.x, cellMin.y, size.x, size.y);

    void OnValidate() {
        // Guard rails
        size.x = Mathf.Max(1, size.x);
        size.y = Mathf.Max(1, size.y);
        cellMin.x = Mathf.Max(0, cellMin.x);
        cellMin.y = Mathf.Max(0, cellMin.y);

        if (GridService.Instance != null) {
            GridService.Instance.ApplyAreaRules();
#if UNITY_EDITOR
            if (GridService.Instance.isActiveAndEnabled)
                GridService.Instance.RebuildFlow();
#endif
        }
    }

    // Scene-view preview (helps when placing areas)
    void OnDrawGizmosSelected() { DrawAreaGizmo(0.35f); }
    void OnDrawGizmos()         { DrawAreaGizmo(0.15f); }

    void DrawAreaGizmo(float a) {
        var grid = GridService.Instance;
        if (grid == null) return;

        var r = CellRect;
        Color c = rule switch {
            AreaRule.WalkOnly  => new Color(0.50f, 0.30f, 0.10f, a), // brown
            AreaRule.BuildOnly => new Color(1.00f, 1.00f, 1.00f, a), // light white
            AreaRule.Forbidden => new Color(1.00f, 0.00f, 0.00f, a), // red
            _                  => new Color(1.00f, 1.00f, 0.00f, a), // yellow (Both)
        };
        Gizmos.color = c;

        float lift = 0.02f;
        int x0 = Mathf.Clamp(r.x, 0, grid.size.x);
        int y0 = Mathf.Clamp(r.y, 0, grid.size.y);
        int x1 = Mathf.Clamp(r.x + r.width, 0, grid.size.x);
        int y1 = Mathf.Clamp(r.y + r.height, 0, grid.size.y);

        for (int y = y0; y < y1; y++) {
            for (int x = x0; x < x1; x++) {
                var cell = new Vector2Int(x, y);
                var ctr = grid.CellCenter(cell) + Vector3.up * lift;
                Gizmos.DrawCube(ctr, new Vector3(grid.cellSize * 0.95f, 0.02f, grid.cellSize * 0.95f));
            }
        }
    }
}
