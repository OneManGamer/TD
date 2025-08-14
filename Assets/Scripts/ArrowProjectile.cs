using System.Collections.Generic;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour, IPoolable
{
    public enum FlightMode { Straight, Ballistic }

    [Header("Flight")]
    public FlightMode flightMode = FlightMode.Straight;
    public float speed = 18f;
    public float gravity = 9.81f;
    public float maxLifetime = 6f;

    [Header("Damage")]
    [SerializeField] float baseDamage = 10f;
    [SerializeField] DamageType damageType = DamageType.Physical;

    [Header("Hit / Stick")]
    [Tooltip("Layers this projectile can hit. Exclude Projectile / Ignore Raycast (and StuckProjectile if you use it).")]
    public LayerMask hitMask = ~0;
    public bool stickOnHit = true;
    [Tooltip("How far the arrow sinks into the surface (meters).")]
    public float stickDepth = 0.06f;
    [Tooltip("Small random cone so arrows don't overlap perfectly.")]
    public float embedJitterDegrees = 4f;
    [Tooltip("Move to safe layer + disable colliders after sticking so future shots won't hit it.")]
    public bool ignoreAfterStick = true;

    [Header("Stick to World (ground/walls)")]
    [Tooltip("If off, arrows that hit non-enemies will not stick (they despawn).")]
    public bool allowStickToWorld = true;
    [Tooltip("If sticking to world, despawn after this many seconds.")]
    public float worldStickLifetime = 3.0f;

    [Header("Visual")]
    public bool orientToVelocity = true;
    [Tooltip("Make unique material instances on stick to avoid any shared-material tint weirdness.")]
    public bool instantiateMaterialsOnStick = true;

    // runtime
    Vector3 _vel;
    float _life;

    // follow enemy (no parenting → no scale inheritance)
    bool _stuckEnemy;
    Transform _carrier;
    Health _carrierHealth;
    Vector3 _localOffset;     // relative to carrier (rotation space)
    Quaternion _localRot;

    // stick to world
    bool _stuckWorld;
    float _worldStickTimer;

    // scale handling
    Vector3 _prefabLocalScale;
    bool _scaleCached;

    // layers & colliders reset
    int _stuckLayer = -1;
    Transform[] _allTransforms;
    int[] _origLayers;

    // on-hit effects (data-driven)
    List<OnHitEffectSO> _onHitEffects;

    // ------------ public API ------------
    public void SetDamage(float amt, DamageType type) { baseDamage = amt; damageType = type; }

    public void SetOnHitEffects(IList<OnHitEffectSO> effects)
    {
        if (effects == null || effects.Count == 0) { _onHitEffects = null; return; }
        _onHitEffects ??= new List<OnHitEffectSO>(effects.Count);
        _onHitEffects.Clear();
        for (int i = 0; i < effects.Count; i++)
            if (effects[i]) _onHitEffects.Add(effects[i]);
    }

    // ------------ Unity ------------
    void Awake()
    {
        if (!_scaleCached)
        {
            _prefabLocalScale = transform.localScale;
            _scaleCached = true;
        }

        // cache transform hierarchy original layers so we can restore after being set to StuckProjectile
        _allTransforms = GetComponentsInChildren<Transform>(true);
        _origLayers = new int[_allTransforms.Length];
        for (int i = 0; i < _allTransforms.Length; i++)
            _allTransforms[i].gameObject.layer = (_origLayers[i] = _allTransforms[i].gameObject.layer);
    }

    // IPoolable — called by ProjectilePool BEFORE SetActive(true)
    public void OnSpawned()
    {
        // Restore original layers if we had changed them while stuck.
        if (_allTransforms != null && _origLayers != null && _allTransforms.Length == _origLayers.Length)
        {
            for (int i = 0; i < _allTransforms.Length; i++)
                _allTransforms[i].gameObject.layer = _origLayers[i];
        }
        _stuckLayer = -1;

        // Re-enable all colliders we might have disabled on stick.
        EnableAllColliders(gameObject);

        // Clear visuals like trails so reused arrows don't smear.
        var tr = GetComponent<TrailRenderer>();
        if (tr) tr.Clear();

        // Reset transient state
        _stuckEnemy = false;
        _stuckWorld = false;
        _worldStickTimer = 0f;
        _carrier = null;
        if (_carrierHealth != null) _carrierHealth.OnDeath -= HandleCarrierDeath;
        _carrierHealth = null;

        // Effects set externally by Fire SO after spawn; we clear here.
        _onHitEffects = null;

        // Ensure intended prefab scale
        if (_scaleCached) transform.localScale = _prefabLocalScale;

        enabled = true;
    }

    public void OnRecycled()
    {
        if (_carrierHealth != null) _carrierHealth.OnDeath -= HandleCarrierDeath;
        _stuckEnemy = false;
        _stuckWorld = false;
        _carrier = null;
        _carrierHealth = null;
        _onHitEffects = null;
        enabled = false;
    }

    public void LaunchToward(Vector3 origin, Vector3 aimPoint, float launchSpeed)
    {
        if (_scaleCached) transform.localScale = _prefabLocalScale; // preserve prefab size

        transform.position = origin;
        Vector3 dir = (aimPoint - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        _vel = dir.normalized * Mathf.Max(0.01f, launchSpeed);
        flightMode = FlightMode.Straight;
        _life = 0f;
    }

    public bool TryLaunchBallistic(Vector3 origin, Vector3 target, float launchSpeed, float g, bool highArc)
    {
        if (_scaleCached) transform.localScale = _prefabLocalScale;

        transform.position = origin;

        Vector3 to = target - origin;
        Vector3 toXZ = new Vector3(to.x, 0f, to.z);
        float x = toXZ.magnitude;
        float y = to.y;
        float v2 = launchSpeed * launchSpeed;
        float gx = g * x;

        float underRoot = v2 * v2 - g * (g * x * x + 2f * y * v2);
        if (underRoot < 0f) return false;

        float root = Mathf.Sqrt(underRoot);
        float angle = Mathf.Atan((v2 + (highArc ? root : -root)) / gx);

        Vector3 dirXZ = (x > 0.0001f) ? toXZ / x : transform.forward;
        Vector3 vel = dirXZ * (launchSpeed * Mathf.Cos(angle)) + Vector3.up * (launchSpeed * Mathf.Sin(angle));

        _vel = vel;
        gravity = g;
        flightMode = FlightMode.Ballistic;
        _life = 0f;

        return true;
    }

    void Update()
    {
        // follow enemy (no parent)
        if (_stuckEnemy)
        {
            if (!_carrier || !_carrier.gameObject.activeInHierarchy)
            {
                Despawn();
                return;
            }

            transform.position = _carrier.position + _carrier.rotation * _localOffset;
            transform.rotation = _carrier.rotation * _localRot;
            return;
        }

        // stuck to world with timeout
        if (_stuckWorld)
        {
            _worldStickTimer += Time.deltaTime;
            if (_worldStickTimer >= worldStickLifetime) { Despawn(); }
            return;
        }

        // free flight
        float dt = Time.deltaTime;
        _life += dt;
        if (_life >= maxLifetime) { Despawn(); return; }

        Vector3 prev = transform.position;
        if (flightMode == FlightMode.Ballistic) _vel += Vector3.down * gravity * dt;

        Vector3 next = prev + _vel * dt;
        Vector3 dir = next - prev;
        float dist = dir.magnitude;

        if (dist > 0f)
        {
            if (Physics.Raycast(prev, dir.normalized, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }
        }

        transform.position = next;
        if (orientToVelocity && _vel.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(_vel.normalized, Vector3.up);
    }

    void OnDisable()
    {
        if (_carrierHealth != null) _carrierHealth.OnDeath -= HandleCarrierDeath;
        _stuckEnemy = false;
        _stuckWorld = false;
        _carrier = null;
        _carrierHealth = null;
        _onHitEffects = null;
    }

    void OnDestroy()
    {
        if (_carrierHealth != null) _carrierHealth.OnDeath -= HandleCarrierDeath;
    }

    void OnHit(RaycastHit hit)
    {
        var info = new DamageInfo
        {
            amount = baseDamage,
            type = damageType,
            critMult = 1f,
            source = gameObject,
            hitPoint = hit.point
        };
        Combat.ApplyHit(hit.collider, info);

        // Run extra data-driven on-hit effects
        if (_onHitEffects != null)
        {
            for (int i = 0; i < _onHitEffects.Count; i++)
            {
                var fx = _onHitEffects[i];
                if (!fx) continue;
                if (fx.chance >= 1f || Random.value <= fx.chance)
                    fx.Apply(hit.collider, info);
            }
        }

        if (!stickOnHit) { Despawn(); return; }

        // final forward (prefer velocity)
        Vector3 fwd = (_vel.sqrMagnitude > 0.0001f) ? _vel.normalized : -hit.normal;

        // jitter so many arrows don't overlap perfectly
        if (embedJitterDegrees > 0f)
        {
            float rad = Mathf.Tan(embedJitterDegrees * Mathf.Deg2Rad);
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(fwd, right);
            Vector2 r = Random.insideUnitCircle * rad;
            fwd = (fwd + right * r.x + up * r.y).normalized;
        }

        Vector3 sinkPos = hit.point - fwd * Mathf.Max(0f, stickDepth);
        transform.SetPositionAndRotation(sinkPos, Quaternion.LookRotation(fwd, Vector3.up));
        if (_scaleCached) transform.localScale = _prefabLocalScale; // keep prefab size

        // determine if we hit an enemy (has Health)
        Transform tCarrier = hit.rigidbody ? hit.rigidbody.transform : hit.collider.transform;
        var hp = tCarrier ? tCarrier.GetComponentInParent<Health>() : null;

        if (hp != null)
        {
            // follow enemy via relative pose (no parenting => no scale stretch)
            _carrier = tCarrier;
            _carrierHealth = hp;
            _carrierHealth.OnDeath += HandleCarrierDeath;

            _localOffset = Quaternion.Inverse(_carrier.rotation) * (sinkPos - _carrier.position);
            _localRot    = Quaternion.Inverse(_carrier.rotation) * transform.rotation;

            _stuckEnemy = true;
            _vel = Vector3.zero;
        }
        else
        {
            // hit world (ground/wall)
            if (allowStickToWorld)
            {
                _stuckWorld = true;
                _worldStickTimer = 0f;
                _vel = Vector3.zero;
            }
            else
            {
                Despawn();
                return;
            }
        }

        // make stuck arrows non-hittable to avoid stacking
        if (ignoreAfterStick)
        {
            _stuckLayer = LayerMask.NameToLayer("StuckProjectile");
            if (_stuckLayer < 0) _stuckLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (_stuckLayer >= 0) SetLayerRecursively(gameObject, _stuckLayer);
            DisableAllColliders(gameObject);
        }

        // avoid shared-material tinting
        if (instantiateMaterialsOnStick)
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) { var _ = r.material; }
        }
    }

    void HandleCarrierDeath()
    {
        Despawn();
    }

    void Despawn()
    {
        if (ProjectilePool.Instance)
            ProjectilePool.Instance.Recycle(this);
        else
            Destroy(gameObject);
    }

    static void DisableAllColliders(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;
    }

    static void EnableAllColliders(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = true;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        var trs = go.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs) t.gameObject.layer = layer;
    }
}
