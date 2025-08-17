using System.Collections.Generic;
using UnityEngine;

public class AttachOnHitEffects : MonoBehaviour
{
    public OnHitEffectSO[] effects;

    void Start()
    {
        var arrow = GetComponent<ArrowProjectile>();
        if (arrow && effects != null && effects.Length > 0)
            arrow.SetOnHitEffects(new List<OnHitEffectSO>(effects));
    }
}
