using System.Collections.Generic;
using UnityEngine;
using TD.Combat;

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
    [Tooltip("Layers this projectile can hit.  Exclude Projectile / Ignore Raycast.")]
    public LayerMask hitMask = ~0;
    public bool stickOnHit = true;
    [Tooltip("How far the arrow sinks into the surface (meters).")]
    public float stickDepth = 0.06f;
    [Tooltip("Small random cone so arrows do not overlap perfectly.")]
    public float embedJitterDegrees = 4f;
    [Tooltip("Move to safe layer and disable colliders after sticking so future shots will not hit it.")]
    public bool ignoreAfterStick = true;

    [Header("Stick to World (ground/walls)")]
    [Tooltip("If off, arrows that hit non-enemies will not stick and will despawn instead.")]
    public bool allowStickToWorld = true;
    [Tooltip("If sticking to world, despawn after this many seconds.")]
    public float worldStickLifetime = 3.0f;

    [Header("Visual")]
    public bool orientToVelocity = true;
    [Tooltip("Make unique material instances on stick to avoid any shared-material tint issues.")]
    public bool instantiateMaterialsOnStick = true;

    // runtime
    Vector3 _vel;
    float _life;
    bool _stuckEnemy;
    bool _stuckWorld;
    float _worldStickTimer;

    Transform _carrier;
    Health _carrierHealth;
    Vector3 _localOffset;
    Quaternion _localRot;

    static bool _scaleCached;
    static Vector3 _prefabLocalScale;

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

    // ------------ Unity and Pool ------------
    void Awake()
    {
        if (!_scaleCached)
        {
            _prefabLocalScale = transform.localScale;
            _scaleCached = true;
        }

        _allTransforms = GetComponentsInChildren<Transform>(true);
        _origLayers = new int[_allTransforms.Length];
        for (int i = 0; i < _allTransforms.Length; i++) _origLayers[i] = _allTransforms[i].gameObject.layer;
        _stuckLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void OnEnable()
    {
        _life = 0f;
        _stuckEnemy = _stuckWorld = false;
        _carrier = null;
        _carrierHealth = null;
        _worldStickTimer = 0f;
        enabled = true;
    }

    // New for pool contract
    void IPoolable.OnSpawned()
    {
        // restore layers and colliders just in case this came back from a stick state
        RestoreOriginalLayers();
        EnableAllColliders(gameObject);

        _life = 0f;
        _stuckEnemy = _stuckWorld = false;
        _carrier = null;
        _carrierHealth = null;
        _worldStickTimer = 0f;

        if (_scaleCached) transform.localScale = _prefabLocalScale;
        enabled = true;
    }

    void IPoolable.OnRecycled()
    {
        _life = 0f;
        _stuckEnemy = _stuckWorld = false;
        _carrier = null;
        _carrierHealth = null;
        _onHitEffects = null;
        enabled = false;
    }

    public void LaunchToward(Vector3 origin, Vector3 aimPoint, float launchSpeed)
    {
        if (_scaleCached) transform.localScale = _prefabLocalScale;

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
        float dist = toXZ.magnitude;
        if (dist < 0.001f) return false;

        float speed2 = launchSpeed * launchSpeed;
        float y = to.y;

        float inside = speed2 * speed2 - g * (g * dist * dist + 2f * y * speed2);
        if (inside < 0f) return false;

        float sqrt = Mathf.Sqrt(inside);
        float angle = highArc ? Mathf.Atan2(speed2 + sqrt, g * dist) : Mathf.Atan2(speed2 - sqrt, g * dist);

        Vector3 dir = toXZ.normalized;
        Vector3 v = dir * Mathf.Cos(angle) * launchSpeed + Vector3.up * Mathf.Sin(angle) * launchSpeed;

        flightMode = FlightMode.Ballistic;
        gravity = Mathf.Abs(g);
        _vel = v;
        _life = 0f;
        return true;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _life += dt;
        if (_life > maxLifetime)
        {
            Despawn();
            return;
        }

        if (_stuckEnemy)
        {
            if (!_carrier)
            {
                Despawn();
                return;
            }
            transform.SetPositionAndRotation(_carrier.position + _carrier.rotation * _localOffset, _carrier.rotation * _localRot);
            return;
        }

        if (_stuckWorld)
        {
            _worldStickTimer += dt;
            if (_worldStickTimer >= worldStickLifetime)
            {
                Despawn();
                return;
            }
            return;
        }

        if (flightMode == FlightMode.Ballistic)
            _vel += Vector3.down * gravity * dt;

        Vector3 prev = transform.position;
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

    void Despawn()
    {
        gameObject.SetActive(false);
    }

    void HandleCarrierDeath()
    {
        _stuckEnemy = false;
        _carrier = null;
        _carrierHealth = null;
        Despawn();
    }

    void OnHit(RaycastHit hit)
    {
        // Apply unified damage
        CombatIntegrationAPI.ApplyHit(gameObject, hit.collider, baseDamage, damageType, hit.point, hit.normal);

        // Run extra data-driven on-hit effects if present
        if (_onHitEffects != null)
        {
            // Note: this DamageInfo is whatever your OnHitEffectSO expects in your project.
            var info = new DamageInfo
            {
                amount = baseDamage,
                type = damageType,
                critMult = 1f,
                source = gameObject,
                hitPoint = hit.point
            };

            for (int i = 0; i < _onHitEffects.Count; i++)
            {
                var fx = _onHitEffects[i];
                if (!fx) continue;
                if (fx.chance >= 1f || Random.value <= fx.chance)
                    fx.Apply(hit.collider, info);
            }
        }

        if (!stickOnHit) { Despawn(); return; }

        // stick logic
        Vector3 fwd = _vel.sqrMagnitude > 0.0001f ? _vel.normalized : transform.forward;

        if (embedJitterDegrees > 0f)
        {
            float j = embedJitterDegrees * 0.5f;
            fwd = Quaternion.Euler(Random.Range(-j, j), Random.Range(-j, j), 0f) * fwd;
        }

        Vector3 sinkPos = hit.point - fwd * Mathf.Max(0f, stickDepth);
        transform.SetPositionAndRotation(sinkPos, Quaternion.LookRotation(fwd, Vector3.up));
        if (_scaleCached) transform.localScale = _prefabLocalScale;

        // determine if we hit an enemy (has Health)
        Transform tCarrier = hit.rigidbody ? hit.rigidbody.transform : hit.collider.transform;
        var hp = tCarrier ? tCarrier.GetComponentInParent<Health>() : null;

        if (hp != null)
        {
            // follow enemy via relative pose
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

                if (instantiateMaterialsOnStick)
                {
                    var rends = GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rends)
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = Instantiate(mats[i]);
                        r.sharedMaterials = mats;
                    }
                }

                if (ignoreAfterStick)
                {
                    SetLayerRecursively(gameObject, _stuckLayer);
                    var cols = GetComponentsInChildren<Collider>(true);
                    foreach (var c in cols) c.enabled = false;
                }
            }
            else
            {
                Despawn();
            }
        }
    }

    void RestoreOriginalLayers()
    {
        if (_allTransforms == null || _origLayers == null) return;
        int len = Mathf.Min(_allTransforms.Length, _origLayers.Length);
        for (int i = 0; i < len; i++)
        {
            var t = _allTransforms[i];
            if (t) t.gameObject.layer = _origLayers[i];
        }
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
