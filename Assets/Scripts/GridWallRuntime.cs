using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class GridWallsRuntime : MonoBehaviour {
    [Header("References")]
    public GridService grid;              // assign (or leave null to auto-grab GridService.Instance)
    public Transform wallsParent;         // optional parent for spawned walls

    [Header("What counts as 'walkable'?")]
    public bool includeDynamicBlocks = false; // false = use CanWalkBase (areas only); true = CanWalk (areas + towers)

    [Header("Wall geometry")]
    public float wallHeight = 1.2f;
    public float wallThickness = 0.12f;

    [Header("Rendering")]
    public bool addRenderer = true;
    public Material wallMaterial = null;  // optional; if null uses default cube material
    public Color wallColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Collider")]
    public bool addCollider = false;      // logic already hard-blocks; colliders are optional
    public bool colliderIsTrigger = true;

    [Header("Lifecycle")]
    public bool autoRebuildOnStart = true;

    readonly List<GameObject> spawned = new();

    void Start() {
        if (autoRebuildOnStart) RebuildWalls();
    }

    [ContextMenu("Rebuild Walls Now")]
    public void RebuildWalls() {
        if (grid == null) grid = GridService.Instance;
        if (grid == null) return;

        // Clear existing
        foreach (var go in spawned) {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        spawned.Clear();

        // Iterate cell edges; place wall where one side is walkable and the other is not
        for (int y = 0; y < grid.size.y; y++) {
            for (int x = 0; x < grid.size.x; x++) {
                var c = new Vector2Int(x, y);
                bool walkThis = includeDynamicBlocks ? grid.CanWalk(c) : grid.CanWalkBase(c);

                // Right-edge boundary (between (x,y) and (x+1,y))
                PlaceIfBoundary(c, new Vector2Int(1, 0), walkThis);

                // Top-edge boundary (between (x,y) and (x,y+1))
                PlaceIfBoundary(c, new Vector2Int(0, 1), walkThis);
            }
        }
    }

    void PlaceIfBoundary(Vector2Int c, Vector2Int off, bool walkThis) {
        var other = new Vector2Int(c.x + off.x, c.y + off.y);
        bool walkOther = grid.InBounds(other) && (includeDynamicBlocks ? grid.CanWalk(other) : grid.CanWalkBase(other));

        if (walkThis == walkOther) return; // no boundary

        Vector3 a = grid.CellCenter(c);
        Vector3 b = grid.InBounds(other) ? grid.CellCenter(other) : (a + new Vector3(off.x * grid.cellSize, 0f, off.y * grid.cellSize));
        Vector3 mid = (a + b) * 0.5f + Vector3.up * (wallHeight * 0.5f);

        bool vertical = (off.x == 1); // edge between left/right cells -> vertical segment
        Vector3 size = vertical
            ? new Vector3(wallThickness, wallHeight, grid.cellSize)
            : new Vector3(grid.cellSize, wallHeight, wallThickness);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = vertical ? $"Wall_V_{c.x}_{c.y}" : $"Wall_H_{c.x}_{c.y}";
        go.transform.position = mid;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = size;
        if (wallsParent != null) go.transform.SetParent(wallsParent, true);

        // Renderer setup
        if (!addRenderer) {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr) { if (Application.isPlaying) Destroy(mr); else DestroyImmediate(mr); }
        } else if (wallMaterial != null) {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr) mr.sharedMaterial = wallMaterial;
        } else {
            // tint default material
            var mr = go.GetComponent<MeshRenderer>();
            if (mr) {
                // Use a unique instance at runtime to avoid editing shared material in editor
                if (Application.isPlaying) mr.material.color = wallColor;
                else mr.sharedMaterial.color = wallColor;
            }
        }

        // Collider setup
        var col = go.GetComponent<BoxCollider>();
        if (!addCollider) {
            if (col) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        } else {
            if (col) col.isTrigger = colliderIsTrigger;
        }

        spawned.Add(go);
    }
}
