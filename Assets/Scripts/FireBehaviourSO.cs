using UnityEngine;

public abstract class FireBehaviourSO : ScriptableObject {
    // Called once when tower begins having a valid target + is allowed to shoot
    public virtual void StartFire(TowerShooter shooter) {}
    // Called on each allowed shot tick (based on shotsPerSecond)
    public abstract void FireTick(TowerShooter shooter, Transform target);
    // Called when tower loses target or stops firing
    public virtual void StopFire(TowerShooter shooter) {}
}
