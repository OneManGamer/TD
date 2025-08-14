// File: HealthBarUIOverlay.cs
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUIOverlay : MonoBehaviour
{
    [Header("Wiring")]
    public Health health;
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    public Image fill;     // child named "Fill"
    public Image bg;       // child named "BG"
    public Canvas canvas;  // UI_Root (Screen Space – Overlay)
    public Camera cam;     // usually Camera.main

    [Header("Behavior")]
    public bool hideWhenFull = true;
    public bool clampToScreen = true;
    public Vector2 clampPadding = new Vector2(8f, 8f);

    RectTransform _rt;
    RectTransform _canvasRT;

    // subscription guard
    bool _subscribed = false;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (!cam) cam = Camera.main;

        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (canvas) _canvasRT = canvas.transform as RectTransform;

        // Ensure Fill is "Filled → Horizontal"
        if (fill && fill.type != Image.Type.Filled)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
        }
    }

    void OnEnable()
    {
        TryAutoWire();
        TrySubscribe();
    }

    void Start()
    {
        // In case spawner set fields after OnEnable
        TrySubscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void TryAutoWire()
    {
        // If spawner forgot to set these, try to fill them
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!cam) cam = Camera.main;
        if (!target && health) target = health.transform;
        if (!health && target) health = target.GetComponent<Health>();
        if (canvas && _canvasRT == null) _canvasRT = canvas.transform as RectTransform;
    }

    void TrySubscribe()
    {
        if (_subscribed) return;
        if (health == null) return;
        health.OnDeath += HandleDeath;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (_subscribed && health != null)
        {
            health.OnDeath -= HandleDeath;
        }
        _subscribed = false;
    }

    void HandleDeath()
    {
        Destroy(gameObject); // remove this UI bar instance
    }

    void LateUpdate()
    {
        // If the target was despawned/reached goal and destroyed, clean up the bar
        if (!target || !canvas || !_rt || !_canvasRT)
        {
            Destroy(gameObject);
            return;
        }

        // In case wiring happened after enable, keep trying to subscribe once
        if (!_subscribed && health != null) TrySubscribe();

        // 1) World → Screen
        if (!cam) cam = Camera.main;
        Vector3 worldPos = target.position + worldOffset;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        // 2) Screen → Canvas local (works for Overlay & Camera modes)
        Vector2 localPos;
        var cvCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null
                    : (canvas.worldCamera ? canvas.worldCamera : cam);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screenPos, cvCam, out localPos);

        // 3) Optional clamp inside the visible canvas rect
        if (clampToScreen)
        {
            Vector2 half = _canvasRT.rect.size * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -half.x + clampPadding.x, half.x - clampPadding.x);
            localPos.y = Mathf.Clamp(localPos.y, -half.y + clampPadding.y, half.y - clampPadding.y);
        }

        _rt.anchoredPosition = localPos;

        // 4) Fill amount & visibility
        if (health)
        {
            float frac = (health.maxHP <= 0f) ? 0f : Mathf.Clamp01(health.currentHP / health.maxHP);
            if (fill) fill.fillAmount = frac;

            bool show = !hideWhenFull || frac < 0.999f;
            if (bg)   bg.enabled = show;
            if (fill) fill.enabled = show;
        }
    }
}
