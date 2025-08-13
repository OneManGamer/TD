using UnityEngine;

public class ArrowProjectile : MonoBehaviour {
    public enum FlightMode { Straight, Ballistic }

    [Header("Flight")]
    public FlightMode flightMode = FlightMode.Straight;
    public float speed = 18f;            // default straight speed or ballistic launch speed
    public float gravity = 9.81f;        // used in Ballistic
    public float maxLifetime = 6f;

    [Header("Damage")]
    public float damage = 10f;
    public LayerMask hitMask = ~0;

    [Header("Visual orientation")]
    public Transform model;              // child mesh; if null uses self
    public Vector3 modelForwardLocal = Vector3.forward;

    [Header("Hit behavior")]
    public bool stickOnHit = true;

    Vector3 _vel;
    float _life;
    Quaternion _modelOffset;

    void Awake() {
        if (model == null) model = transform;
        _modelOffset = Quaternion.FromToRotation(Vector3.forward, modelForwardLocal.normalized);
    }

    public void Launch(Vector3 origin, Vector3 initialVelocity, FlightMode mode) {
        transform.position = origin;
        _vel = initialVelocity;
        flightMode = mode;
        _life = 0f;
        OrientToVelocity();
    }

    public void LaunchToward(Vector3 origin, Vector3 target, float straightSpeed) {
        Vector3 dir = (target - origin);
        dir.y = 0f; // flat shot
        Vector3 v0 = dir.normalized * Mathf.Max(0.01f, straightSpeed);
        Launch(origin, v0, FlightMode.Straight);
    }

    // Low/high arc ballistic to a static target; returns false if unreachable at given speed
    public bool TryLaunchBallistic(Vector3 origin, Vector3 target, float launchSpeed, float g, bool highArc = false) {
        Vector3 to = target - origin;
        Vector3 toXZ = new Vector3(to.x, 0f, to.z);
        float xz = toXZ.magnitude;
        float y  = to.y;
        if (xz < 0.0001f) { Launch(origin, Vector3.up * launchSpeed, FlightMode.Ballistic); return true; }

        float v2 = launchSpeed * launchSpeed;
        float under = v2 * v2 - g * (g * xz * xz + 2f * y * v2);
        if (under < 0f) return false;

        float root = Mathf.Sqrt(under);
        float tanTheta = (v2 + (highArc ? +root : -root)) / (g * xz);
        float angle = Mathf.Atan(tanTheta);

        Vector3 dirXZ = toXZ.normalized;
        Vector3 v0 = dirXZ * (launchSpeed * Mathf.Cos(angle)) + Vector3.up * (launchSpeed * Mathf.Sin(angle));
        Launch(origin, v0, FlightMode.Ballistic);
        return true;
    }

    void Update() {
        float dt = Time.deltaTime;
        _life += dt;
        if (_life >= maxLifetime) { Destroy(gameObject); return; }

        Vector3 pos = transform.position;

        if (flightMode == FlightMode.Ballistic) {
            _vel += Vector3.down * gravity * dt;
        }

        Vector3 nextPos = pos + _vel * dt;

        // hit detection along the path
        Vector3 step = nextPos - pos;
        float dist = step.magnitude;
        if (dist > 0f && Physics.Raycast(pos, step.normalized, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore)) {
            OnHit(hit);
            return;
        }

        transform.position = nextPos;
        OrientToVelocity();
    }

    void OrientToVelocity() {
        if (_vel.sqrMagnitude < 0.0001f) return;
        model.rotation = Quaternion.LookRotation(_vel.normalized, Vector3.up) * _modelOffset;
    }

    void OnHit(RaycastHit hit) {
        var h = hit.collider.GetComponentInParent<Health>();
        if (h != null) h.TakeDamage(damage);

        if (stickOnHit) {
            transform.position = hit.point;
            _vel = Vector3.zero;
            OrientToVelocity();
            transform.SetParent(hit.collider.transform, true);
            enabled = false; // keep stuck
        } else {
            Destroy(gameObject);
        }
    }
}
