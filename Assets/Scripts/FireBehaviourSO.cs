// File: FireBehaviourSO.cs
using UnityEngine;

public abstract class FireBehaviourSO : ScriptableObject
{
    public virtual void StartFire(TowerShooter shooter) { }
    public virtual void StopFire(TowerShooter shooter) { }
    public abstract void FireTick(TowerShooter shooter, Transform target);
}
