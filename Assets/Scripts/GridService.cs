using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class GridService : MonoBehaviour {
    public static GridService Instance { get; private set; }

    [Header("Grid Settings")]
    public Vector2Int size = new Vector2Int(30, 20);
    public float cellSize = 1f;
    public Vector3 origin = new Vector3(-15f, 0f, -10f);

    [Header("Routing")]
    public Vector2Int goalCell;
    public List<Vector2Int> spawnCells = new();

    [Header("Editor")]
    public bool ShowGizmos = false;

    [Header("Path Preferences (soft, center of lane)")]
    public bool preferCenter = true;                 // turn off to get raw shortest paths
    [Range(0, 6)] public int preferCenterClearance = 2; // desired clearance in cells (0=tolerate walls)
    [Range(0, 40)] public int clearanceCostWeight = 10; // cost per missing-clearance cell

    // ---- Runtime fields ----
    // dynamic occupancy
    bool[] blocked;      // true = tower placed here (not walkable)
    // flow / costs
    int[] dist;          // Dijkstra distance to goal
    Vector2[] dir;       // flow direction per cell (x,z)
    int[] penalty;       // proximity bias around blocked cells (bends paths early)
    // static area permissions
    bool[] walkPerm;     // allowed to walk here (areas)
    bool[] buildPerm;    // allowed to build here (areas)
    // clearance (for center-line steering)
    int[] clearDist;     // distance in cells to nearest closed cell
    Vector2[] clearDir;  // direction to more clearance (away from walls)

    void Awake()      { Instance = this; EnsureInit(); }
    void OnEnable()   { EnsureInit(); ApplyAreaRules(); }
    void OnValidate() { EnsureInit(); ApplyAreaRules(); }

    void EnsureInit() {
        int len = Mathf.Max(1, size.x * size.y);

        if (blocked   == null || blocked.Length   != len) blocked   = new bool[len];
        if (dist      == null || dist.Length      != len) dist      = new int[len];
        if (dir       == null || dir.Length       != len) dir       = new Vector2[len];
        if (penalty   == null || penalty.Length   != len) penalty   = new int[len];
        if (walkPerm  == null || walkPerm.Length  != len) walkPerm  = new bool[len];
        if (buildPerm == null || buildPerm.Length != len) buildPerm = new bool[len];
        if (clearDist == null || clearDist.Length != len) clearDist = new int[len];
        if (clearDir  == null || clearDir.Length  != len) clearDir  = new Vector2[len];

        // default static permissions = BOTH everywhere
        for (int i = 0; i < len; i++) { walkPerm[i] = true; buildPerm[i] = true; }
    }

    int Idx(Vector2Int c) => c.y * size.x + c.x;
    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < size.x && c.y < size.y;

    public Vector3 CellCenter(Vector2Int c)
        => origin + new Vector3((c.x + 0.5f) * cellSize, 0f, (c.y + 0.5f) * cellSize);

    public Vector2Int WorldToCell(Vector3 w) {
        Vector3 p = w - origin;
        return new Vector2Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.z / cellSize));
    }

    // ---- Dynamic occupancy (towers) ----
    public bool IsBlocked(Vector2Int c) => blocked[Idx(c)];
    public void SetBlocked(Vector2Int c, bool v) { blocked[Idx(c)] = v; }
    public void ClearAllBlocks() { System.Array.Clear(blocked, 0, blocked.Length); }

    // ---- Static permissions (areas) ----
    public bool CanWalkBase(Vector2Int c)  => InBounds(c) && walkPerm[Idx(c)];
    public bool CanBuildBase(Vector2Int c) => InBounds(c) && buildPerm[Idx(c)];
    public bool CanWalk(Vector2Int c)      => InBounds(c) && walkPerm[Idx(c)] && !IsBlocked(c);
    public bool CanBuild(Vector2Int c)     => InBounds(c) && buildPerm[Idx(c)] && !IsBlocked(c);
    bool ClosedForWalk(Vector2Int c)       => !InBounds(c) || !walkPerm[Idx(c)] || IsBlocked(c);

    // ---- Public queries ----
    public Vector2 Flow(Vector2Int c)         => dir[Idx(c)];
    public int     Distance(Vector2Int c)     => dist[Idx(c)];
    public Vector2 ClearanceDir(Vector2Int c) => clearDir[Idx(c)];
    public int     Clearance(Vector2Int c)    => clearDist[Idx(c)];

    // -------- PRIORITY-BASED AREA APPLICATION (higher wins) --------
    public void ApplyAreaRules() {
        EnsureInit();

        int len = Mathf.Max(1, size.x * size.y);
        if (walkPerm == null || walkPerm.Length != len) walkPerm = new bool[len];
        if (buildPerm == null || buildPerm.Length != len) buildPerm = new bool[len];

        // reset to BOTH
        for (int i = 0; i < len; i++) { walkPerm[i] = true; buildPerm[i] = true; }

        // gather and sort GridAreas by priority (higher applied last)
#if UNITY_2023_1_OR_NEWER
        var areasArr = FindObjectsByType<GridArea>(FindObjectsSortMode.None);
#else
        var areasArr = FindObjectsOfType<GridArea>();
#endif
        var areas = new List<GridArea>(areasArr);
        areas.Sort((a, b) => {
            int c = a.priority.CompareTo(b.priority); // low -> high
            return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID()); // stable tie-break
        });

        foreach (var a in areas) {
            var r = a.CellRect;

            // clamp to grid bounds
            int x0 = Mathf.Clamp(r.x, 0, size.x);
            int y0 = Mathf.Clamp(r.y, 0, size.y);
            int x1 = Mathf.Clamp(r.x + r.width, 0, size.x);
            int y1 = Mathf.Clamp(r.y + r.height, 0, size.y);
            if (x1 <= x0 || y1 <= y0) continue;

            for (int y = y0; y < y1; y++) {
                for (int x = x0; x < x1; x++) {
                    int i = Idx(new Vector2Int(x, y));
                    switch (a.rule) {
                        case GridArea.AreaRule.Both:      walkPerm[i] = true;  buildPerm[i] = true;  break;
                        case GridArea.AreaRule.WalkOnly:  walkPerm[i] = true;  buildPerm[i] = false; break;
                        case GridArea.AreaRule.BuildOnly: walkPerm[i] = false; buildPerm[i] = true;  break;
                        case GridArea.AreaRule.Forbidden: walkPerm[i] = false; buildPerm[i] = false; break;
                    }
                }
            }
        }
    }

    // ---- Path shaping: penalty field makes creeps bend early from towers ----
    void BuildPenaltyField(int radius = 2, int baseWeight = 4, int cap = 16) {
        System.Array.Clear(penalty, 0, penalty.Length);

        for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++) {
            var c = new Vector2Int(x, y);
            if (ClosedForWalk(c) || !IsBlocked(c)) continue; // radiate only from dynamic blocks that are in open space

            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++) {
                if (dx == 0 && dy == 0) continue;
                var n = new Vector2Int(x + dx, y + dy);
                if (!InBounds(n) || ClosedForWalk(n)) continue;

                int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (manhattan > radius) continue;

                int add = (radius - manhattan + 1) * baseWeight;
                int idx = Idx(n);
                penalty[idx] = Mathf.Min(cap, penalty[idx] + add);
            }
        }
    }

    // ---- Clearance field: distance-from-walls & direction to corridor center ----
    void BuildClearanceField() {
        for (int i = 0; i < clearDist.Length; i++) { clearDist[i] = int.MaxValue; clearDir[i] = Vector2.zero; }

        var q = new Queue<Vector2Int>();

        // seed = all CLOSED cells (areas or dynamic blocks) at distance 0
        for (int y = 0; y < size.y; y++) {
            for (int x = 0; x < size.x; x++) {
                var c = new Vector2Int(x, y);
                if (ClosedForWalk(c)) { clearDist[Idx(c)] = 0; q.Enqueue(c); }
            }
        }

        // brushfire outward through OPEN cells (4-neighbors)
        Vector2Int[] n4 = { new(1,0), new(-1,0), new(0,1), new(0,-1) };
        while (q.Count > 0) {
            var c = q.Dequeue();
            int cd = clearDist[Idx(c)];
            foreach (var off in n4) {
                var nc = c + off;
                if (!InBounds(nc) || ClosedForWalk(nc)) continue;
                int ni = Idx(nc);
                if (clearDist[ni] > cd + 1) { clearDist[ni] = cd + 1; q.Enqueue(nc); }
            }
        }

        // gradient toward *higher* clearance (away from walls)
        Vector2Int[] n8 = {
            new( 1,  0), new(-1,  0), new( 0,  1), new( 0, -1),
            new( 1,  1), new( 1, -1), new(-1,  1), new(-1, -1)
        };
        for (int y = 0; y < size.y; y++) {
            for (int x = 0; x < size.x; x++) {
                var c = new Vector2Int(x, y);
                int i = Idx(c);
                if (clearDist[i] == int.MaxValue) { clearDir[i] = Vector2.zero; continue; }

                Vector2 g = Vector2.zero;
                foreach (var off in n8) {
                    var nc = c + off;
                    if (!InBounds(nc)) continue;
                    int ni = Idx(nc);
                    int dd = clearDist[ni] - clearDist[i]; // positive = neighbor safer
                    if (dd > 0) g += new Vector2(off.x, off.y) * dd;
                }
                clearDir[i] = g.sqrMagnitude > 0.0001f ? g.normalized : Vector2.zero;
            }
        }
    }

    // ---- Rebuild full flow (call after placement or area changes) ----
    public void RebuildFlow() {
        EnsureInit();
        ApplyAreaRules();

        for (int i = 0; i < dist.Length; i++) { dist[i] = int.MaxValue; dir[i] = Vector2.zero; }
        if (!InBounds(goalCell) || ClosedForWalk(goalCell)) return;

        // fields
        BuildPenaltyField(radius: 2, baseWeight: 4, cap: 16);
        BuildClearanceField();

        // 8-neighbor Dijkstra (10 straight, 14 diag) + no diagonal corner-cut
        Vector2Int[] neigh = {
            new( 1,  0), new(-1,  0), new( 0,  1), new( 0, -1),
            new( 1,  1), new( 1, -1), new(-1,  1), new(-1, -1)
        };
        int[] moveCost = { 10, 10, 10, 10, 14, 14, 14, 14 };

        var open = new List<Vector2Int>(size.x * size.y);
        var closed = new bool[dist.Length];

        dist[Idx(goalCell)] = 0;
        open.Add(goalCell);

        while (open.Count > 0) {
            // pick lowest dist (naive; fine for small grids)
            int bestI = 0, bestD = int.MaxValue;
            for (int i = 0; i < open.Count; i++) {
                int d = dist[Idx(open[i])];
                if (d < bestD) { bestD = d; bestI = i; }
            }
            var c = open[bestI];
            open.RemoveAt(bestI);
            int ci = Idx(c);
            if (closed[ci]) continue;
            closed[ci] = true;

            for (int k = 0; k < neigh.Length; k++) {
                var off = neigh[k];
                var nc = c + off;
                if (!InBounds(nc) || ClosedForWalk(nc)) continue;

                // no diagonal through two blocked orthogonals
                if (k >= 4) {
                    var a = new Vector2Int(c.x + off.x, c.y);
                    var b = new Vector2Int(c.x, c.y + off.y);
                    if ((InBounds(a) && ClosedForWalk(a)) && (InBounds(b) && ClosedForWalk(b))) continue;
                }

                int ni = Idx(nc);
                int nd = dist[ci] + moveCost[k] + penalty[ni];

                // ---- soft center cost: prefer having at least 'preferCenterClearance' cells from walls ----
                if (preferCenter) {
                    int miss = Mathf.Max(0, preferCenterClearance - clearDist[ni]); // 0 if enough clearance
                    if (miss > 0) nd += miss * clearanceCostWeight;
                }

                if (nd < dist[ni]) { dist[ni] = nd; open.Add(nc); }
            }
        }

        // smooth flow = negative distance gradient (8-neighbors)
        for (int y = 0; y < size.y; y++) {
            for (int x = 0; x < size.x; x++) {
                var c = new Vector2Int(x, y);
                int i = Idx(c);
                if (dist[i] == int.MaxValue) { dir[i] = Vector2.zero; continue; }

                Vector2 g = Vector2.zero;
                foreach (var off in neigh) {
                    var nc = c + off;
                    if (!InBounds(nc)) continue;
                    int ni = Idx(nc);
                    if (dist[ni] == int.MaxValue) continue;
                    int dd = dist[i] - dist[ni]; // positive if neighbor is closer to goal
                    if (dd > 0) g += new Vector2(off.x, off.y) * dd;
                }
                dir[i] = g.sqrMagnitude > 0.0001f ? g.normalized : Vector2.zero;
            }
        }
    }

    // ---- Placement validator: ensure at least one route from every spawn ----
    public bool AllSpawnsReachGoalIf(Vector2Int tempBlock, bool blockedState) {
        EnsureInit();
        ApplyAreaRules();

        bool prev = InBounds(tempBlock) ? IsBlocked(tempBlock) : false;
        if (InBounds(tempBlock)) SetBlocked(tempBlock, blockedState);

        bool ok = true;
        var seen = new bool[size.x * size.y];
        var q = new Queue<Vector2Int>();

        if (InBounds(goalCell) && !ClosedForWalk(goalCell)) {
            q.Enqueue(goalCell);
            seen[Idx(goalCell)] = true;
        }

        Vector2Int[] n4 = { new(1,0), new(-1,0), new(0,1), new(0,-1) };
        while (q.Count > 0) {
            var c = q.Dequeue();
            foreach (var off in n4) {
                var nc = c + off;
                if (!InBounds(nc) || ClosedForWalk(nc)) continue;
                int i = Idx(nc);
                if (!seen[i]) { seen[i] = true; q.Enqueue(nc); }
            }
        }

        foreach (var s in spawnCells) {
            if (!InBounds(s) || !seen[Idx(s)]) { ok = false; break; }
        }

        if (InBounds(tempBlock)) SetBlocked(tempBlock, prev);
        return ok;
    }

    [ContextMenu("Refresh Areas Now")]
    public void RefreshAreasNow() {
        ApplyAreaRules();
        RebuildFlow();
    }

    // ---- Gizmo helpers (Scene/Game) ----
    void OnDrawGizmos() {
        if (!ShowGizmos) return;
        EnsureInit();

        // grid lines
        Gizmos.color = new Color(0f, 0f, 0f, 0.1f);
        float lift = 0.02f;
        for (int y = 0; y < size.y; y++) {
            for (int x = 0; x < size.x; x++) {
                Vector3 center = origin + new Vector3((x + 0.5f) * cellSize, lift, (y + 0.5f) * cellSize);
                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.01f, cellSize));
            }
        }

        // goal & spawns
        if (InBounds(goalCell)) {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawCube(CellCenter(goalCell) + Vector3.up * lift, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
        }
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        foreach (var s in spawnCells) if (InBounds(s))
            Gizmos.DrawCube(CellCenter(s) + Vector3.up * lift, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
    }

    void Start() { RebuildFlow(); }
}
