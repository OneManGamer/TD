using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyAgentFlow : MonoBehaviour {
    [Header("Movement")]
    public float moveSpeed = 2.2f;

    [Header("Cornering")]
    public float lookAheadCells = 1.2f;        // a bit farther so we start turning earlier
    public float cornerMinSpeedFactor = 0.7f;  // slow less at corners
    public float cornerEaseStartDeg = 8f;
    public float cornerEaseEndDeg   = 50f;

    [Header("Stay centered in corridors")]
    public float centerBiasWeight = 0.65f;     // stronger base bias

    [Header("Crowd spacing")]
    public float personalSpace = 0.75f;
    public float separationRadius = 1.0f;
    public float separationStrength = 6.0f;
    public float separationFalloff = 0.6f;

    [Header("Following brake")]
    public float frontConeAngle = 60f;
    public float brakeStartDist = 1.2f;
    public float brakeStopDist  = 0.5f;
    public float brakeMaxFactor = 0.35f;

    const float MaxStepFracOfCell = 0.40f;     // slightly smaller microsteps to reduce edge touches
    Collider[] _buf = new Collider[24];

    void Update() {
        var grid = GridService.Instance;
        if (grid == null) return;

        var curCell = grid.WorldToCell(transform.position);
        if (!grid.CanWalk(curCell)) {
            var safe = FindNearestWalkable(curCell, grid, 3);
            transform.position = grid.CellCenter(safe);
            curCell = safe;
        }

        if (curCell == grid.goalCell) {
            LevelController.Instance.OnEnemyReachedGoal();
            Destroy(gameObject);
            return;
        }

        // ---- Flow now & two look-ahead samples for smoother anticipation ----
        Vector3 flowNow   = SampleFlowBilinear(grid, transform.position);
        if (flowNow.sqrMagnitude < 0.0001f)
            flowNow = (grid.CellCenter(grid.goalCell) - transform.position).normalized;

        Vector3 aheadPos1 = transform.position + flowNow.normalized * (lookAheadCells * grid.cellSize);
        Vector3 flowA1    = SampleFlowBilinear(grid, aheadPos1);
        if (flowA1.sqrMagnitude < 0.0001f) flowA1 = flowNow;

        // a second, farther peek dampens sharp changes
        Vector3 aheadPos2 = aheadPos1 + flowA1.normalized * (0.6f * lookAheadCells * grid.cellSize);
        Vector3 flowA2    = SampleFlowBilinear(grid, aheadPos2);
        if (flowA2.sqrMagnitude < 0.0001f) flowA2 = flowA1;

        // Blend toward the future direction
        float turnAng = Vector3.Angle(flowNow, flowA1);
        float cornerT  = Mathf.InverseLerp(cornerEaseStartDeg, cornerEaseEndDeg, turnAng);
        Vector3 steerDir = Vector3.Slerp(flowNow, flowA1, 0.5f * cornerT);
        steerDir = Vector3.Slerp(steerDir, flowA2, 0.35f * cornerT).normalized;

        // ---- Center bias (bilinear) now + ahead; stronger when near walls ----
        int cdistCell = grid.Clearance(curCell);
        float nearWallBoost = 1f / (1f + cdistCell); // 1, 0.5, 0.33, ...

        Vector3 cNow   = SampleClearDirBilinear(grid, transform.position);
        Vector3 cAhead = SampleClearDirBilinear(grid, aheadPos1);

        Vector3 centerBias = (cNow + cAhead) * 0.5f * (centerBiasWeight * nearWallBoost);

        Vector3 desiredDir = steerDir + centerBias;
        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();
        else desiredDir = steerDir;

        // ---- Crowd avoidance & following brake ----
        int n = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, _buf);
        Vector3 push = Vector3.zero;
        float speedMul = Mathf.Lerp(1f, cornerMinSpeedFactor, Mathf.Clamp01(cornerT));

        for (int i = 0; i < n; i++) {
            var col = _buf[i];
            if (col == null) continue;
            var go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;
            if (go == gameObject || !go.CompareTag("Enemy")) continue;

            Vector3 toOther = go.transform.position - transform.position;
            float dist = toOther.magnitude;
            if (dist < 0.0001f) continue;

            // repel if too close
            float target = personalSpace;
            float overlap = Mathf.Max(0f, target - dist);
            if (overlap > 0f) {
                float w = separationStrength * Mathf.Lerp(1f, separationFalloff, overlap / target);
                push += (-toOther / dist) * overlap * w;
            }

            // brake if ahead
            float ang = Vector3.Angle(desiredDir, toOther);
            if (ang < frontConeAngle && dist < brakeStartDist) {
                float t = Mathf.InverseLerp(brakeStartDist, brakeStopDist, dist);
                float brake = Mathf.Lerp(brakeMaxFactor, 0f, t);
                speedMul = Mathf.Min(speedMul, 1f - brake);
            }
        }

        Vector3 steer = desiredDir + push * Time.deltaTime;
        if (steer.sqrMagnitude > 0.0001f) steer.Normalize();
        Vector3 delta = steer * (moveSpeed * speedMul) * Time.deltaTime;

        transform.position = MoveWithGridCollision(transform.position, delta, grid);
    }

    // --- Bilinear sampling of flow for smooth corners ---
    Vector3 SampleFlowBilinear(GridService grid, Vector3 worldPos) {
        Vector3 p = worldPos - grid.origin;
        float gx = p.x / grid.cellSize;
        float gy = p.z / grid.cellSize;

        int x0 = Mathf.FloorToInt(gx);
        int y0 = Mathf.FloorToInt(gy);
        float tx = Mathf.Clamp01(gx - x0);
        float ty = Mathf.Clamp01(gy - y0);

        x0 = Mathf.Clamp(x0, 0, grid.size.x - 1);
        y0 = Mathf.Clamp(y0, 0, grid.size.y - 1);
        int x1 = Mathf.Min(x0 + 1, grid.size.x - 1);
        int y1 = Mathf.Min(y0 + 1, grid.size.y - 1);

        Vector2 f00 = grid.Flow(new Vector2Int(x0, y0));
        Vector2 f10 = grid.Flow(new Vector2Int(x1, y0));
        Vector2 f01 = grid.Flow(new Vector2Int(x0, y1));
        Vector2 f11 = grid.Flow(new Vector2Int(x1, y1));

        Vector2 fx0 = Vector2.Lerp(f00, f10, tx);
        Vector2 fx1 = Vector2.Lerp(f01, f11, tx);
        Vector2 f   = Vector2.Lerp(fx0, fx1, ty);

        if (f.sqrMagnitude > 0.0001f) f.Normalize();
        return new Vector3(f.x, 0f, f.y);
    }

    // --- Bilinear sampling of clearance direction (away from walls) ---
    Vector3 SampleClearDirBilinear(GridService grid, Vector3 worldPos) {
        Vector3 p = worldPos - grid.origin;
        float gx = p.x / grid.cellSize;
        float gy = p.z / grid.cellSize;

        int x0 = Mathf.FloorToInt(gx);
        int y0 = Mathf.FloorToInt(gy);
        float tx = Mathf.Clamp01(gx - x0);
        float ty = Mathf.Clamp01(gy - y0);

        x0 = Mathf.Clamp(x0, 0, grid.size.x - 1);
        y0 = Mathf.Clamp(y0, 0, grid.size.y - 1);
        int x1 = Mathf.Min(x0 + 1, grid.size.x - 1);
        int y1 = Mathf.Min(y0 + 1, grid.size.y - 1);

        Vector2 c00 = grid.ClearanceDir(new Vector2Int(x0, y0));
        Vector2 c10 = grid.ClearanceDir(new Vector2Int(x1, y0));
        Vector2 c01 = grid.ClearanceDir(new Vector2Int(x0, y1));
        Vector2 c11 = grid.ClearanceDir(new Vector2Int(x1, y1));

        Vector2 cx0 = Vector2.Lerp(c00, c10, tx);
        Vector2 cx1 = Vector2.Lerp(c01, c11, tx);
        Vector2 c   = Vector2.Lerp(cx0, cx1, ty);

        if (c.sqrMagnitude > 0.0001f) c.Normalize();
        return new Vector3(c.x, 0f, c.y);
    }

    Vector3 MoveWithGridCollision(Vector3 pos, Vector3 delta, GridService grid) {
        float cell = Mathf.Max(0.001f, grid.cellSize);
        float maxStep = MaxStepFracOfCell * cell;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / maxStep));
        Vector3 step = delta / steps;

        for (int s = 0; s < steps; s++) {
            var curCell = grid.WorldToCell(pos);
            Vector3 target = pos + step;
            var tgtCell = grid.WorldToCell(target);

            if (tgtCell == curCell || grid.CanWalk(tgtCell)) {
                pos = target;
                continue;
            }
            Vector3 stepX = new Vector3(step.x, 0f, 0f);
            Vector3 stepZ = new Vector3(0f, 0f, step.z);

            var cellX = grid.WorldToCell(pos + stepX);
            if (cellX == curCell || grid.CanWalk(cellX)) pos += stepX;

            var startAfterX = grid.WorldToCell(pos);
            var cellZ = grid.WorldToCell(pos + stepZ);
            if (cellZ == startAfterX || grid.CanWalk(cellZ)) pos += stepZ;
        }
        return pos;
    }

    Vector2Int FindNearestWalkable(Vector2Int from, GridService grid, int maxRadius) {
        if (grid.CanWalk(from)) return from;
        for (int r = 1; r <= maxRadius; r++) {
            for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) {
                if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                var c = new Vector2Int(from.x + dx, from.y + dy);
                if (grid.InBounds(c) && grid.CanWalk(c)) return c;
            }
        }
        return from;
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
