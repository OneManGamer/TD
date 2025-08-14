// File: VelocityTracker.cs
using UnityEngine;

[DisallowMultipleComponent]
public class VelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }

    Vector3 _lastPos;
    float _lastTime;

    void OnEnable()
    {
        _lastPos = transform.position;
        _lastTime = Time.time;
        Velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        float now = Time.time;
        float dt = now - _lastTime;
        if (dt > 0f)
        {
            Vector3 pos = transform.position;
            Vector3 v = (pos - _lastPos) / dt;
            // de-noise tiny jitter
            Velocity = (v.sqrMagnitude < 1e-6f) ? Vector3.zero : v;
            _lastPos = pos;
            _lastTime = now;
        }
    }
}
