using System.Collections;
using UnityEngine;
using TD.Combat;

/// <summary>
/// Lives on the enemy while DoT is active and applies ticks via the unified API.
/// Multiple DoTs can coexist; each call starts its own routine.
/// </summary>
public class DoTAttachment : MonoBehaviour
{
    public void StartDoT(float tickDamage, int ticks, float interval, DamageType type, GameObject source)
    {
        StopAllCoroutines(); // optional: comment out if you want multiple stacks from the same effect instance
        StartCoroutine(Run(tickDamage, ticks, interval, type, source));
    }

    IEnumerator Run(float tickDamage, int ticks, float interval, DamageType type, GameObject source)
    {
        var col = GetComponent<Collider>();
        if (!col) yield break;

        for (int i = 0; i < ticks; i++)
        {
            if (!this) yield break;
            if (col)
            {
                CombatIntegrationAPI.ApplyHit(
                    source,
                    col,
                    tickDamage,
                    type,
                    transform.position,
                    Vector3.up
                );
            }
            yield return new WaitForSeconds(Mathf.Max(0.01f, interval));
        }
        // Auto-remove runner if no other coroutines are left
        if (this) Destroy(this);
    }
}
