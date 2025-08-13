using UnityEngine;

public class TowerShooter : MonoBehaviour {
    // Back-compat shim so older Fire*SO that call shooter.Targeting.* still work
    public class TargetingShim {
        private readonly TowerShooter owner;
        public TargetingShim(TowerShooter o) { owner = o; }
        public bool HasTarget => owner.CurrentTarget != null;
        public Transform currentTarget => owner.CurrentTarget;
        public Vector3 AimPoint() => owner.CurrentTarget
            ? owner.CurrentTarget.position + Vector3.up * 0.6f
            : owner.transform.position;
    }
    public TargetingShim Targeting { get; private set; }

    [Header("Data (optional)")]
    public TowerDefinitionSO towerDef;        // if set, overrides fields below

    [Header("Setup")]
    public Transform rotatePivot;
    public Transform firePoint;
    public FireBehaviourSO fireBehaviour;     // used if no towerDef or def has none
    public TargetingPolicySO targetingPolicy; // used if no towerDef or def has none

    [Header("Stats (used if no towerDef)")]
    public float range = 8f;
    public float shotsPerSecond = 2f;
    public float turnSpeedDegPerSec = 360f;
    public float minFireAngle = 8f;

    [Header("Target Scan")]
    public LayerMask enemyMask = ~0;
    public float retargetCooldown = 0.25f;

    [Header("FX")]
    public AudioSource sfxShot;

    Transform _currentTarget;
    float _retargetTimer;
    float _cooldown;
    bool _wasFiring;

    Collider[] _scanBuf = new Collider[48];

    // resolved data
    float _rRange, _rSps, _rTurn, _rMinAngle;
    FireBehaviourSO _rFire;
    TargetingPolicySO _rPolicy;

    public Transform CurrentTarget => _currentTarget;

    void Awake() {
        Targeting = new TargetingShim(this);

        // Resolve data (towerDef wins if provided)
        _rRange    = (towerDef != null) ? towerDef.range              : range;
        _rSps      = (towerDef != null) ? towerDef.shotsPerSecond     : shotsPerSecond;
        _rTurn     = (towerDef != null) ? towerDef.turnSpeedDegPerSec : turnSpeedDegPerSec;
        _rMinAngle = (towerDef != null) ? towerDef.minFireAngle       : minFireAngle;

        _rFire   = (towerDef != null && towerDef.fireBehaviour   != null) ? towerDef.fireBehaviour   : fireBehaviour;
        _rPolicy = (towerDef != null && towerDef.targetingPolicy != null) ? towerDef.targetingPolicy : targetingPolicy;
    }

    void Update() {
        if (_rFire == null || rotatePivot == null || firePoint == null) return;

        // retarget
        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer <= 0f) { _retargetTimer = retargetCooldown; Retarget(); }

        bool hasTarget = _currentTarget != null;
        bool canFireNow = false;

        if (hasTarget) {
            Vector3 aim = AimPoint(_currentTarget);
            Vector3 dir = aim - rotatePivot.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) {
                Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
                rotatePivot.rotation = Quaternion.RotateTowards(rotatePivot.rotation, want, _rTurn * Time.deltaTime);
            }
            float ang = Vector3.Angle(rotatePivot.forward, (aim - rotatePivot.position).normalized);
            canFireNow = ang <= _rMinAngle;
        }

        if (canFireNow && !_wasFiring) _rFire.StartFire(this);
        else if (!canFireNow && _wasFiring) _rFire.StopFire(this);
        _wasFiring = canFireNow;

        if (canFireNow) {
            _cooldown -= Time.deltaTime;
            float tick = 1f / Mathf.Max(0.01f, _rSps);
            while (_cooldown <= 0f) {
                _rFire.FireTick(this, _currentTarget);
                _cooldown += tick;
            }
        } else {
            _cooldown = 0f;
        }
    }

    void Retarget() {
        if (_rPolicy != null) {
            int n = Physics.OverlapSphereNonAlloc(transform.position, _rRange, _scanBuf, enemyMask, QueryTriggerInteraction.Ignore);
            _currentTarget = _rPolicy.ChooseTarget(this, _scanBuf, n);
        } else {
            int n = Physics.OverlapSphereNonAlloc(transform.position, _rRange, _scanBuf, enemyMask, QueryTriggerInteraction.Ignore);
            Transform best = null; float bestD2 = float.MaxValue; Vector3 p = transform.position;
            for (int i = 0; i < n; i++) { var c = _scanBuf[i]; if (!c) continue;
                var t = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
                float d2 = (t.position - p).sqrMagnitude; if (d2 < bestD2) { bestD2 = d2; best = t; } }
            _currentTarget = best;
        }
    }

    Vector3 AimPoint(Transform t) => t ? t.position + Vector3.up * 0.6f : transform.position;
}
