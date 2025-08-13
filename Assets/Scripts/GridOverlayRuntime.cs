using UnityEngine;

[ExecuteAlways]
public class GridOverlayRuntime : MonoBehaviour {
    [Header("What to draw")]
    public bool drawGrid = true;
    public bool drawAreas = true;
    public bool drawSpawns = true;
    public bool drawGoal = true;

    [Header("Area Colors (A = transparency)")]
    public Color walkOnlyColor = new Color(0.50f, 0.30f, 0.10f, 0.40f); // brown
    public Color buildOnlyColor = new Color(1.00f, 1.00f, 1.00f, 0.28f); // light white
    public Color bothColor      = new Color(1.00f, 1.00f, 0.00f, 0.28f); // yellow
    public Color forbiddenColor = new Color(1.00f, 0.00f, 0.00f, 0.40f); // red

    [Header("Grid/Lines")]
    public Color gridColor = new Color(0f, 0f, 0f, 0.25f);

    [Header("Render")]
    public float yLift = 0.02f;                 // lift above ground to avoid z-fighting
    public Material overrideMaterial = null;    // optional: assign your own Unlit/Transparent

    static Material _lineMat;

    void EnsureMaterial() {
        if (overrideMaterial != null) return;
        if (_lineMat != null) return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        _lineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _lineMat.SetInt("_ZWrite", 0);
    }

    void OnRenderObject() {
        var grid = GridService.Instance;
        if (grid == null) return;

        EnsureMaterial();
        var mat = overrideMaterial != null ? overrideMaterial : _lineMat;
        if (mat == null) return;

        mat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        Vector3 origin = grid.origin + new Vector3(0f, yLift, 0f);
        float w = grid.size.x * grid.cellSize;
        float h = grid.size.y * grid.cellSize;

        // --- Area tints (filled quads) ---
        if (drawAreas) {
            for (int y = 0; y < grid.size.y; y++) {
                for (int x = 0; x < grid.size.x; x++) {
                    var c = new Vector2Int(x, y);
                    Color col;
                    // Determine area type from static permissions
                    bool walk = grid.CanWalkBase(c);
                    bool build = grid.CanBuildBase(c);
                    if (walk && build) col = bothColor;
                    else if (walk && !build) col = walkOnlyColor;
                    else if (!walk && build) col = buildOnlyColor;
                    else col = forbiddenColor;

                    DrawFilledCell(grid, c, col, 0.94f);
                }
            }
        }

        // --- Grid lines ---
        if (drawGrid) {
            GL.Begin(GL.LINES);
            GL.Color(gridColor);
            // Vertical
            for (int x = 0; x <= grid.size.x; x++) {
                float xx = x * grid.cellSize;
                GL.Vertex(origin + new Vector3(xx, 0f, 0f));
                GL.Vertex(origin + new Vector3(xx, 0f, h));
            }
            // Horizontal
            for (int y = 0; y <= grid.size.y; y++) {
                float zz = y * grid.cellSize;
                GL.Vertex(origin + new Vector3(0f, 0f, zz));
                GL.Vertex(origin + new Vector3(w, 0f, zz));
            }
            GL.End();
        }

        // --- Goal cell ---
        if (drawGoal && grid.InBounds(grid.goalCell)) {
            DrawFilledCell(grid, grid.goalCell, new Color(1f, 0.2f, 0.2f, 0.55f), 0.88f);
        }

        // --- Spawn cells ---
        if (drawSpawns && grid.spawnCells != null) {
            foreach (var s in grid.spawnCells) {
                if (grid.InBounds(s)) DrawFilledCell(grid, s, new Color(0.2f, 0.6f, 1f, 0.55f), 0.88f);
            }
        }

        GL.PopMatrix();
    }

    void DrawFilledCell(GridService grid, Vector2Int c, Color col, float inset = 0.95f) {
        Vector3 center = grid.CellCenter(c) + new Vector3(0f, yLift * 1.5f, 0f);
        float hs = grid.cellSize * 0.5f * inset;
        Vector3 a = center + new Vector3(-hs, 0f, -hs);
        Vector3 b = center + new Vector3( hs, 0f, -hs);
        Vector3 d = center + new Vector3(-hs, 0f,  hs);
        Vector3 e = center + new Vector3( hs, 0f,  hs);

        GL.Begin(GL.TRIANGLES);
        GL.Color(col);
        GL.Vertex(a); GL.Vertex(b); GL.Vertex(e);
        GL.Vertex(a); GL.Vertex(e); GL.Vertex(d);
        GL.End();
    }
}
