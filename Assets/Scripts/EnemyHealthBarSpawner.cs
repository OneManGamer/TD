// File: EnemyHealthBarSpawner.cs
using UnityEngine;
using UnityEngine.UI; // only used for Image auto-wiring

[DisallowMultipleComponent]
public class EnemyHealthBarSpawner : MonoBehaviour
{
    [Header("Prefab & Canvas")]
    public GameObject healthBarPrefab;  // assign pf_HealthBarUI (no Canvas on it)
    public Canvas overlayCanvas;        // assign UI_Root (Screen Space - Overlay)
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    void Start()
    {
        if (!healthBarPrefab)
        {
            Debug.LogWarning($"{name}: No healthBarPrefab assigned.");
            return;
        }

        // Try to find the overlay canvas if not wired
        if (!overlayCanvas)
        {
            var tagged = GameObject.FindWithTag("MainUI");
            overlayCanvas = tagged ? tagged.GetComponent<Canvas>() : null;

            if (!overlayCanvas)
            {
                // Unity 2023/6: prefer new APIs; fall back for older Unity
                #if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                overlayCanvas = Object.FindFirstObjectByType<Canvas>();
                if (!overlayCanvas) overlayCanvas = Object.FindAnyObjectByType<Canvas>();
                #else
                overlayCanvas = Object.FindObjectOfType<Canvas>();
                #endif
            }
        }

        if (!overlayCanvas)
        {
            Debug.LogWarning($"{name}: No overlay Canvas found.");
            return;
        }

        var h = GetComponent<Health>();
        if (!h)
        {
            Debug.LogWarning($"{name}: No Health component; cannot spawn health bar.");
            return;
        }

        var go = Instantiate(healthBarPrefab, overlayCanvas.transform);

        // Ensure the behaviour exists
        var hb = go.GetComponent<HealthBarUIOverlay>();
        if (!hb) hb = go.AddComponent<HealthBarUIOverlay>();

        // Wire required refs
        hb.canvas = overlayCanvas;
        hb.cam = Camera.main;
        hb.health = h;
        hb.target = transform;
        hb.worldOffset = offset;

        // Auto-wire BG/Fill by name if not set on the prefab
        if (!hb.fill)
        {
            var imgs = go.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.name.Equals("Fill", System.StringComparison.OrdinalIgnoreCase))
                { hb.fill = img; break; }
            }
        }
        if (!hb.bg)
        {
            var imgs = go.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.name.Equals("BG", System.StringComparison.OrdinalIgnoreCase))
                { hb.bg = img; break; }
            }
        }
    }
}
