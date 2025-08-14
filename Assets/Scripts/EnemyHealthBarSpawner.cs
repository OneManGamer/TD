// File: EnemyHealthBarSpawner.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyHealthBarSpawner : MonoBehaviour, IPoolable
{
    [Header("References")]
    [Tooltip("UI bar prefab with HealthBarUIOverlay component (must be on the prefab ROOT).")]
    [SerializeField] private HealthBarUIOverlay overlayPrefab;

    [Tooltip("Screen Space - Overlay canvas that will host the bars (leave blank on prefab; auto-find at runtime).")]
    [SerializeField] private Canvas overlayCanvas;

    [Tooltip("Camera used to convert world -> screen points. Optional for Screen Space - Overlay (leave blank on prefab).")]
    [SerializeField] private Camera worldCamera;

    [Header("Placement")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    [Header("Lifecycle")]
    [SerializeField] private bool spawnOnEnable = true;

    [Header("Optional Fallbacks (used only if fields are left empty)")]
    [SerializeField] private bool autoFindCanvasIfMissing = true;
    [SerializeField] private bool autoFindCameraIfMissing = true;

    private Health _health;
    private HealthBarUIOverlay _instance;
    private bool _subscribed;

    void Awake()
    {
        _health = GetComponent<Health>();
    }

    void OnEnable()
    {
        TrySubscribe();
        if (spawnOnEnable) EnsureBar();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
        DestroyBar();
    }

    public void EnsureBar()
    {
        // If we already have one or we don't have a prefab assigned, just bail.
        if (_instance || !overlayPrefab) return;

        // One-time fallbacks (prefab can’t hold scene refs; we find them at runtime)
        if (!overlayCanvas && autoFindCanvasIfMissing)
        {
            overlayCanvas = FindCanvasOnce();
        }

        if (!overlayCanvas)
        {
            Debug.LogWarning($"{name}: No overlay Canvas found/assigned; cannot spawn health bar.");
            return;
        }

        if (!worldCamera && autoFindCameraIfMissing)
        {
            worldCamera = Camera.main; // ok to be null for Screen Space - Overlay
        }

        // Choose camera depending on canvas mode
        var camToUse = (overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : (overlayCanvas.worldCamera ? overlayCanvas.worldCamera : worldCamera);

        // Instantiate under the canvas and wire references
        _instance = Instantiate(overlayPrefab, overlayCanvas.transform, false);
        _instance.name = $"HealthBarUIOverlay_{gameObject.name}";
        _instance.canvas = overlayCanvas;
        _instance.cam    = camToUse;
        _instance.health = _health;
        _instance.target = transform;
        _instance.worldOffset = worldOffset;
    }

    public void DestroyBar()
    {
        if (_instance)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }

    // ---- IPoolable ----
    public void OnSpawned()
    {
        EnsureBar();
        if (_instance)
        {
            _instance.health = _health;
            _instance.target = transform;
            _instance.canvas = overlayCanvas ? overlayCanvas : _instance.canvas;
            // keep current cam unless we have an explicit one to set
            if (worldCamera) _instance.cam = worldCamera;
            _instance.worldOffset = worldOffset;
        }
    }

    public void OnRecycled()
    {
        DestroyBar();
    }

    // ---- Health wiring ----
    void TrySubscribe()
    {
        if (_subscribed || _health == null) return;
        _health.OnDeath += HandleDeath;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (_subscribed && _health != null)
            _health.OnDeath -= HandleDeath;
        _subscribed = false;
    }

    void HandleDeath()
    {
        DestroyBar();
    }

    // ---- Cross-version Canvas finder ----
    private static Canvas FindCanvasOnce()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude);
#else
        return FindObjectOfType<Canvas>();
#endif
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!_health) _health = GetComponent<Health>();
    }
#endif
}
