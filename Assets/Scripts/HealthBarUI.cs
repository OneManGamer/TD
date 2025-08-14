// File: HealthBarUI.cs
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Wiring")]
    public Health health;
    public Transform followTarget;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    public Image fill;                 // assign the Fill image
    public Canvas rootCanvas;          // assign the Canvas on this GO

    [Header("Behavior")]
    public bool billboard = true;
    public bool hideWhenFull = true;
    public float minSize = 0.9f;       // clamp size vs distance
    public float maxSize = 1.4f;

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        if (!rootCanvas) rootCanvas = GetComponentInChildren<Canvas>(true);
        if (!fill) fill = transform.GetComponentInChildren<Image>(true);
    }

    void LateUpdate()
    {
        if (!health || !followTarget) return;

        // Position
        transform.position = followTarget.position + worldOffset;

        // Billboard
        if (billboard && _cam)
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);

        // Fill
        float frac = (health.maxHP <= 0f) ? 0f : Mathf.Clamp01(health.currentHP / health.maxHP);
        if (fill) fill.fillAmount = frac;

        // Hide when full
        if (rootCanvas && hideWhenFull)
            rootCanvas.enabled = frac < 0.999f;

        // Optional size clamp by distance so it stays readable
        if (_cam)
        {
            float d = Vector3.Distance(_cam.transform.position, transform.position);
            float s = Mathf.Clamp(1f + (d - 8f) * 0.02f, minSize, maxSize);
            transform.localScale = Vector3.one * s;
        }
    }
}
