// File: EnemyAgentFlow.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(VelocityTracker))]
public class EnemyAgentFlow : MonoBehaviour, IPoolable

{
    [Header("Movement")]
    public float moveSpeed = 2.2f;

    [Header("Cornering")]
    public float lookAheadCells = 1.2f;
    public float cornerMinSpeedFactor = 0.7f;
    public float cornerEaseStartDeg = 8f;
    public float cornerEaseEndDeg = 50f;

    [Header("Stay centered in corridors")]
    public float centerBiasWeight = 0.65f;

    [Header("Crowd spacing (steering)")]
    public float personalSpace = 0.75f;           // desired minimum distance
    public float separationRadius = 1.0f;         // how far we look for neighbors
    public float separationStrength = 6.0f;       // steering weight
    public float separationFalloff = 0.6f;        // softer as we get closer to target spacing
    [Tooltip("Only check these layers for spacing (set this to the Enemy layer).")]
    public LayerMask separationMask = 0;

    [Header("Crowd spacing (post-move resolution)")]
    [Tooltip("Extra push after moving to separate overlapped agents.")]
    public int resolveOverlapIterations = 2;
    public float resolveMaxPushPerSecond = 4.0f;  // units/sec of max separation correction

    [Header("Following brake")]
    public float frontConeAngle = 60f;
    public float brakeStartDist = 1.2f;
    public float brakeStopDist = 0.5f;
    public float brakeMaxFactor = 0.35f;

    const float MaxStepFracOfCell = 0.40f;
    Collider[] _buf = new Collider[64]; // larger buffer for crowds

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb) { _rb.isKinematic = true; _rb.useGravity = false; } // we move by transform
    }
public void OnSpawned()  // IPoolable
{
    // Ensure the enemy starts fresh when reused from the pool.
    var h = GetComponent<Health>();
    if (h)
    {
        h.currentHP = Mathf.Max(1f, h.maxHP);
    }

    // If you drive stats from an EnemyDefinitionSO via EnemySetup, reapply here too.
    var setup = GetComponent<EnemySetup>();
    if (setup && setup.definition)
    {
        // Health
        var d = setup.definition;
        if (h)
        {
            h.maxHP = Mathf.Max(1f, d.maxHP);
            h.currentHP = h.maxHP;
            h.resistances = d.resistances;
        }

        // Move speed
        // EnemySetup already writes in Awake on first spawn.  We mirror it for pooled spawns.
        var mover = this; // EnemyAgentFlow
        mover.moveSpeed = d.moveSpeed * Mathf.Max(0.01f, setup.moveSpeedMultiplier);
    }

    enabled = true;
}

public void OnRecycled()  // IPoolable
{
    enabled = false;
}
    void Update()
    {
        var grid = GridService.Instance;
        if (!grid) return;

        var curCell = grid.WorldToCell(transform.position);
        if (!grid.CanWalk(curCell))
        {
            var safe = FindNearestWalkable(curCell, grid, 3);
            transform.position = grid.CellCenter(safe);
            curCell = safe;
        }

        if (curCell == grid.goalCell)
        {
            // Reached the goal.  Notify level, then recycle with safe fallback.
            LevelController.Instance.OnEnemyReachedGoal();
            if (EnemyPool.Instance != null) EnemyPool.Instance.Recycle(this); else Destroy(gameObject);
            return;
        }

        // ---------- flow + look-ahead ----------
        Vector3 flowNow = SampleFlowBilinear(grid, transform.position);
        if (flowNow.sqrMagnitude < 0.0001f)
            flowNow = (grid.CellCenter(grid.goalCell) - transform.position).normalized;

        Vector3 aheadPos1 = transform.position + flowNow.normalized * (lookAheadCells * grid.cellSize);
        Vector3 flowA1 = SampleFlowBilinear(grid, aheadPos1); if (flowA1.sqrMagnitude < 0.0001f) flowA1 = flowNow;
        Vector3 aheadPos2 = aheadPos1 + flowA1.normalized * (0.6f * lookAheadCells * grid.cellSize);
        Vector3 flowA2 = SampleFlowBilinear(grid, aheadPos2); if (flowA2.sqrMagnitude < 0.0001f) flowA2 = flowA1;

        float turnAng = Vector3.Angle(flowNow, flowA1);
        float cornerT = Mathf.InverseLerp(cornerEaseStartDeg, cornerEaseEndDeg, turnAng);
        Vector3 steerDir = Vector3.Slerp(flowNow, flowA1, 0.5f * cornerT);
        steerDir = Vector3.Slerp(steerDir, flowA2, 0.35f * cornerT).normalized;

        // ---------- center bias ----------
        int cdistCell = grid.Clearance(curCell);
        float nearWallBoost = 1f / (1f + cdistCell);
        Vector3 cNow = SampleClearDirBilinear(grid, transform.position);
        Vector3 cAhead = SampleClearDirBilinear(grid, aheadPos1);
        Vector3 centerBias = (cNow + cAhead) * 0.5f * (centerBiasWeight * nearWallBoost);

        Vector3 desiredDir = steerDir + centerBias;
        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize(); else desiredDir = steerDir;

        // ---------- neighbor spacing (steer away) ----------
        int n = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            _buf,
            separationMask == 0 ? ~0 : separationMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 push = Vector3.zero;
        float speedMul = Mathf.Lerp(1f, cornerMinSpeedFactor, Mathf.Clamp01(cornerT));

        for (int i = 0; i < n; i++)
        {
            var col = _buf[i];
            if (!col) continue;
            var go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;
            if (go == gameObject || !go.CompareTag("Enemy")) continue;

            Vector3 toOther = go.transform.position - transform.position;
            float dist = toOther.magnitude;
            if (dist < 0.0001f) continue;

            // repel if closer than desired spacing
            float overlap = Mathf.Max(0f, personalSpace - dist);
            if (overlap > 0f)
            {
                // stronger when very close; fall off as we approach target spacing
                float w = separationStrength * Mathf.Lerp(1f, separationFalloff, overlap / personalSpace);
                push += (-toOther / dist) * overlap * w;
            }

            // brake if following too closely
            float ang = Vector3.Angle(desiredDir, toOther);
            if (ang < frontConeAngle && dist < brakeStartDist)
            {
                float t = Mathf.InverseLerp(brakeStartDist, brakeStopDist, dist);
                float brake = Mathf.Lerp(brakeMaxFactor, 0f, t);
                speedMul = Mathf.Min(speedMul, 1f - brake);
            }
        }

        // NOTE: do NOT multiply push by dt here; treat it as a direction weight.
        Vector3 steer = desiredDir + push;
        if (steer.sqrMagnitude > 0.0001f) steer.Normalize();

        Vector3 delta = steer * (moveSpeed * speedMul) * Time.deltaTime;

        // move with grid collision
        Vector3 newPos = MoveWithGridCollision(transform.position, delta, grid);

        // ---------- post-move overlap resolution ----------
        newPos = ResolveOverlaps(newPos, grid, Time.deltaTime);

        transform.position = newPos;
        // (Rigidbody stays kinematic.  VelocityTracker supplies velocity for aiming.)
    }

    // --- Bilinear sampling helpers (unchanged) ---
    Vector3 SampleFlowBilinear(GridService grid, Vector3 worldPos)
    {
        Vector3 p = worldPos - grid.origin;
        float gx = p.x / grid.cellSize, gy = p.z / grid.cellSize;
        int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
        float tx = Mathf.Clamp01(gx - x0), ty = Mathf.Clamp01(gy - y0);
        x0 = Mathf.Clamp(x0, 0, grid.size.x - 1); y0 = Mathf.Clamp(y0, 0, grid.size.y - 1);
        int x1 = Mathf.Min(x0 + 1, grid.size.x - 1), y1 = Mathf.Min(y0 + 1, grid.size.y - 1);
        Vector2 f00 = grid.Flow(new Vector2Int(x0, y0)), f10 = grid.Flow(new Vector2Int(x1, y0));
        Vector2 f01 = grid.Flow(new Vector2Int(x0, y1)), f11 = grid.Flow(new Vector2Int(x1, y1));
        Vector2 fx0 = Vector2.Lerp(f00, f10, tx), fx1 = Vector2.Lerp(f01, f11, tx);
        Vector2 f = Vector2.Lerp(fx0, fx1, ty);
        if (f.sqrMagnitude > 0.0001f) f.Normalize();
        return new Vector3(f.x, 0f, f.y);
    }

    Vector3 SampleClearDirBilinear(GridService grid, Vector3 worldPos)
    {
        Vector3 p = worldPos - grid.origin;
        float gx = p.x / grid.cellSize, gy = p.z / grid.cellSize;
        int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
        float tx = Mathf.Clamp01(gx - x0), ty = Mathf.Clamp01(gy - y0);
        x0 = Mathf.Clamp(x0, 0, grid.size.x - 1); y0 = Mathf.Clamp(y0, 0, grid.size.y - 1);
        int x1 = Mathf.Min(x0 + 1, grid.size.x - 1), y1 = Mathf.Min(y0 + 1, grid.size.y - 1);
        Vector2 c00 = grid.ClearanceDir(new Vector2Int(x0, y0)), c10 = grid.ClearanceDir(new Vector2Int(x1, y0));
        Vector2 c01 = grid.ClearanceDir(new Vector2Int(x0, y1)), c11 = grid.ClearanceDir(new Vector2Int(x1, y1));
        Vector2 cx0 = Vector2.Lerp(c00, c10, tx), cx1 = Vector2.Lerp(c01, c11, tx);
        Vector2 c = Vector2.Lerp(cx0, cx1, ty);
        if (c.sqrMagnitude > 0.0001f) c.Normalize();
        return new Vector3(c.x, 0f, c.y);
    }

    // --- Overlap resolver: gentle, grid-respecting push apart ---
    Vector3 ResolveOverlaps(Vector3 pos, GridService grid, float dt)
    {
        if (resolveOverlapIterations <= 0 || resolveMaxPushPerSecond <= 0f) return pos;

        float maxPush = resolveMaxPushPerSecond * dt;

        for (int it = 0; it < resolveOverlapIterations; it++)
        {
            int n = Physics.OverlapSphereNonAlloc(
                pos, personalSpace, _buf,
                separationMask == 0 ? ~0 : separationMask,
                QueryTriggerInteraction.Ignore
            );

            Vector3 totalPush = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                var col = _buf[i];
                if (!col) continue;
                var go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;
                if (go == gameObject || !go.CompareTag("Enemy")) continue;

                Vector3 toOther = go.transform.position - pos;
                float dist = toOther.magnitude;
                if (dist < 1e-4f) continue;

                float overlap = personalSpace - dist;
                if (overlap > 0f)
                {
                    totalPush += (-toOther / dist) * (overlap * 0.5f); // split the difference
                }
            }

            if (totalPush.sqrMagnitude < 1e-6f) break;

            Vector3 pushStep = Vector3.ClampMagnitude(totalPush, maxPush);
            Vector3 next = MoveWithGridCollision(pos, pushStep, grid);
            if ((next - pos).sqrMagnitude < 1e-8f) break; // stuck
            pos = next;
        }

        return pos;
    }

    Vector3 MoveWithGridCollision(Vector3 pos, Vector3 delta, GridService grid)
    {
        float cell = Mathf.Max(0.001f, grid.cellSize);
        float maxStep = MaxStepFracOfCell * cell;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / maxStep));
        Vector3 step = delta / steps;

        for (int s = 0; s < steps; s++)
        {
            var curCell = grid.WorldToCell(pos);
            Vector3 target = pos + step;
            var tgtCell = grid.WorldToCell(target);

            if (tgtCell == curCell || grid.CanWalk(tgtCell))
            {
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

    Vector2Int FindNearestWalkable(Vector2Int from, GridService grid, int maxRadius)
    {
        if (grid.CanWalk(from)) return from;
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    var c = new Vector2Int(from.x + dx, from.y + dy);
                    if (grid.InBounds(c) && grid.CanWalk(c)) return c;
                }
        }
        return from;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
