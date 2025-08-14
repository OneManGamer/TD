// File: TowerShooter.cs
using UnityEngine;

public class TowerShooter : MonoBehaviour
{
    [Header("Definition")]
    public TowerDefinitionSO definition;

    [Header("Scene Refs")]
    public Transform yawPivot;         // optional
    public Transform pitchPivot;       // optional
    public Transform firePoint;        // required for projectiles
    public AudioSource sfxShot;        // optional

    [Header("Targeting")]
    public LayerMask enemyMask = ~0;
    public string enemyTag = "Enemy";
    public float scanInterval = 0.15f;

    public Transform CurrentTarget { get; private set; }

    Collider[] _scanBuf = new Collider[64];
    float _scanTimer;
    float _shotTimer;
    float _cooldown;
    float _sqrRange;
    bool _firing;

    void Awake()
    {
        ApplyDefinition();
    }

    void OnValidate()
    {
        if (definition) ApplyDefinition();
    }

    void ApplyDefinition()
    {
        if (!definition) return;
        _cooldown = 1f / Mathf.Max(0.01f, definition.shotsPerSecond);
        _sqrRange = definition.range * definition.range;
    }

    void Update()
    {
        if (!definition || definition.fireBehaviour == null) return;
        float dt = Time.deltaTime;

        _scanTimer -= dt;
        if (_scanTimer <= 0f) { _scanTimer = scanInterval; ScanForTarget(); }

        if (CurrentTarget)
        {
            Vector3 aim = CurrentTarget.position + Vector3.up * 0.6f;
            Transform yawT = yawPivot ? yawPivot : transform;
            Vector3 to = aim - yawT.position;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion targetYaw = Quaternion.LookRotation(flat.normalized, Vector3.up);
                yawT.rotation = Quaternion.RotateTowards(yawT.rotation, targetYaw, definition.turnSpeedDegPerSec * dt);
            }
            if (pitchPivot)
            {
                Vector3 localAim = pitchPivot.InverseTransformPoint(aim);
                float pitchDeg = -Mathf.Atan2(localAim.y, Mathf.Sqrt(localAim.x * localAim.x + localAim.z * localAim.z)) * Mathf.Rad2Deg;
                Quaternion qPitch = Quaternion.Euler(pitchDeg, 0f, 0f);
                pitchPivot.localRotation = Quaternion.RotateTowards(pitchPivot.localRotation, qPitch, definition.turnSpeedDegPerSec * dt);
            }

            float angle = Vector3.Angle(yawT.forward, (aim - yawT.position).normalized);
            bool canShoot = angle <= definition.minFireAngle;

            _shotTimer -= dt;
            if (canShoot && _shotTimer <= 0f)
            {
                if (definition.ammo && !definition.ammo.infinite && !TrySpendAmmo(definition.ammo.perShot))
                {
                    return; // out of ammo
                }

                _shotTimer = _cooldown;
                if (!_firing) { _firing = true; definition.fireBehaviour.StartFire(this); }
                definition.fireBehaviour.FireTick(this, CurrentTarget);
            }
        }
        else if (_firing)
        {
            _firing = false;
            definition.fireBehaviour.StopFire(this);
        }
    }

    void ScanForTarget()
    {
        Vector3 p = transform.position;
        int n = Physics.OverlapSphereNonAlloc(p, definition.range, _scanBuf, enemyMask, QueryTriggerInteraction.Ignore);

        int count = 0;
        for (int i = 0; i < n; i++)
        {
            var c = _scanBuf[i]; if (!c) continue;
            Transform t = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
            if (!string.IsNullOrEmpty(enemyTag) && !t.CompareTag(enemyTag)) continue;
            float d2 = (t.position - p).sqrMagnitude;
            if (d2 > _sqrRange) continue;
            _scanBuf[count++] = c;
        }

        if (definition.targetingPolicy)
            CurrentTarget = definition.targetingPolicy.ChooseTarget(this, _scanBuf, count);
        else
            CurrentTarget = null;

        if (CurrentTarget)
        {
            float d2 = (CurrentTarget.position - p).sqrMagnitude;
            if (d2 > _sqrRange) CurrentTarget = null;
        }
    }

    bool TrySpendAmmo(int amount)
    {
        // Stub for future resource system.  Always allow for now.
        return true;
    }

    public float RollCrit()
    {
        if (!definition || definition.critChance <= 0f) return 1f;
        return Random.value < definition.critChance ? Mathf.Max(1f, definition.critMultiplier) : 1f;
    }
}
