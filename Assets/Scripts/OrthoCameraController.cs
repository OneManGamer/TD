using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthoCameraController : MonoBehaviour {
    [Header("Enable/Disable Inputs")]
    public bool keyboardPanEnabled = true;
    public bool mouseDragPanEnabled = true;

    [Header("Pan Speeds")]
    public float panSpeed = 12f;          // WASD / Arrow keys
    public float dragSpeed = 50f;         // Mouse drag base speed
    public float dragMouseBoost = 2.0f;   // Extra multiplier while dragging with RMB/MMB or LMB+Space
    public float boostMult = 2f;          // Hold Shift to boost

    [Header("Zoom (Mouse Wheel)")]
    public float zoomSpeed = 10f;         // Adjust this to change zoom scroll speed
    public float minSize = 8f;
    public float maxSize = 28f;

    [Header("Clamp (World Bounds)")]
    public Vector2 clampX = new(-16f, 16f);
    public Vector2 clampZ = new(-12f, 12f);

    Camera cam;
    Vector3 lastMouse;

    void Awake() { cam = GetComponent<Camera>(); }

    void Update() {
        float boost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? boostMult : 1f;
        Vector3 move = Vector3.zero;

        // --- Keyboard pan ---
        if (keyboardPanEnabled) {
            float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
            float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
            move += new Vector3(h, 0f, v) * panSpeed * boost * Time.deltaTime;
        }

        // --- Mouse drag pan: RMB/MMB or (Space + LMB) ---
        bool dragging = mouseDragPanEnabled &&
                        (Input.GetMouseButton(1) || Input.GetMouseButton(2) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.Space)));

        if (mouseDragPanEnabled && (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.Space)))) {
            lastMouse = Input.mousePosition;
        }

        if (dragging) {
            Vector3 delta = Input.mousePosition - lastMouse;
            float dragMult = dragSpeed * dragMouseBoost * boost * Time.deltaTime / 100f * (cam.orthographicSize / 10f);
            move += new Vector3(-delta.x, 0f, -delta.y) * dragMult; // drag scene under cursor
            lastMouse = Input.mousePosition;
        }

        // --- Apply translation and clamp ---
        transform.position += move;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, clampX.x, clampX.y);
        p.z = Mathf.Clamp(p.z, clampZ.x, clampZ.y);
        transform.position = p;

        // --- Zoom (mouse wheel), speed adjustable via zoomSpeed ---
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f) {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed * Time.deltaTime * 60f,
                minSize, maxSize
            );
        }
    }
}
