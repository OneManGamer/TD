// File: DamageInfo.cs
using UnityEngine;

public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public float critMult;      // 1 = no crit
    public GameObject source;   // tower or projectile GO
    public Vector3 hitPoint;
}
