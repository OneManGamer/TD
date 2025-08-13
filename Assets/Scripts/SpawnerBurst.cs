using System.Collections;
using UnityEngine;

public class SpawnerBurst : MonoBehaviour {
    public enum SpawnDistribution { Random, RoundRobinGroups, SplitGroupEvenly }

    [Header("What to spawn")]
    public GameObject enemyPrefab;
    public int totalEnemies = 30;

    [Header("Burst settings")]
    public int groupSize = 3;
    public float intraDelay = 0.18f;      // delay between enemies within a group
    public float groupInterval = 2.5f;    // delay between groups
    public float spawnJitter = 0.20f;     // max random offset, auto-clamped to cell

    [Header("Where to spawn")]
    public SpawnDistribution distribution = SpawnDistribution.Random;
    public int fixedSpawnIndex = -1;      // >=0 forces a specific spawn cell (GridService.spawnCells index)

    int spawned;
    int rrIndex; // round-robin index

    void OnEnable() {
        spawned = 0;
        rrIndex = 0;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop() {
        while (GridService.Instance == null) yield return null;

        var grid = GridService.Instance;
        if (enemyPrefab == null) { Debug.LogError("SpawnerBurst: enemyPrefab not set."); yield break; }
        if (grid.spawnCells == null || grid.spawnCells.Count == 0) { Debug.LogError("SpawnerBurst: no spawnCells on GridService."); yield break; }

        // Build flow once up-front (safe even if already built)
        grid.RebuildFlow();

        while (spawned < totalEnemies) {
            int remaining = totalEnemies - spawned;
            int batch = Mathf.Min(groupSize, remaining);

            // CHOOSE SPAWN(S) FOR THIS GROUP
            if (fixedSpawnIndex >= 0 && fixedSpawnIndex < grid.spawnCells.Count) {
                // All in this group from one fixed lane
                Vector3 basePos = grid.CellCenter(grid.spawnCells[fixedSpawnIndex]);
                for (int i = 0; i < batch; i++) {
                    SpawnOne(enemyPrefab, basePos);
                    spawned++;
                    if (i < batch - 1) yield return new WaitForSeconds(intraDelay);
                }
            }
            else if (distribution == SpawnDistribution.SplitGroupEvenly) {
                // Spread this group across all spawns in order
                for (int i = 0; i < batch; i++) {
                    int idx = i % grid.spawnCells.Count;
                    Vector3 basePos = grid.CellCenter(grid.spawnCells[idx]);
                    SpawnOne(enemyPrefab, basePos);
                    spawned++;
                    if (i < batch - 1) yield return new WaitForSeconds(intraDelay);
                }
            }
            else if (distribution == SpawnDistribution.RoundRobinGroups) {
                // Whole group from one spawn, alternating each group
                int idx = rrIndex % grid.spawnCells.Count;
                rrIndex++;
                Vector3 basePos = grid.CellCenter(grid.spawnCells[idx]);
                for (int i = 0; i < batch; i++) {
                    SpawnOne(enemyPrefab, basePos);
                    spawned++;
                    if (i < batch - 1) yield return new WaitForSeconds(intraDelay);
                }
            }
            else { // Random
                int idx = Random.Range(0, grid.spawnCells.Count);
                Vector3 basePos = grid.CellCenter(grid.spawnCells[idx]);
                for (int i = 0; i < batch; i++) {
                    SpawnOne(enemyPrefab, basePos);
                    spawned++;
                    if (i < batch - 1) yield return new WaitForSeconds(intraDelay);
                }
            }

            if (spawned < totalEnemies) yield return new WaitForSeconds(groupInterval);
        }
    }

    // Always spawns at the exact center of a walkable cell; jitter is clamped to stay inside the cell.
    void SpawnOne(GameObject prefab, Vector3 basePos) {
        var grid = GridService.Instance;
        var cell = grid.WorldToCell(basePos);

        if (!grid.CanWalkBase(cell)) {
            Debug.LogWarning($"SpawnerBurst: spawn cell {cell} is not walkable by area rules; skipping spawn.");
            return;
        }

        Vector3 center = grid.CellCenter(cell);

        // Jitter stays inside the cell footprint
        float maxJitter = Mathf.Min(spawnJitter, grid.cellSize * 0.45f);
        Vector2 j2 = (maxJitter > 0f) ? Random.insideUnitCircle * maxJitter : Vector2.zero;
        Vector3 jitter = new Vector3(j2.x, 0f, j2.y);

        var go = Instantiate(prefab, center + jitter, Quaternion.identity);
        go.tag = "Enemy";
    }
}
