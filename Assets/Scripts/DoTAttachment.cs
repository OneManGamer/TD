using System.Collections;
using UnityEngine;
using TD.Combat;
/// <summary>
/// Lives on the enemy while DoT is active and applies ticks via Combat.ApplyHit.
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
        if (!col) col = GetComponentInChildren<Collider>();
        for (int i = 0; i < ticks; i++)
        {
            if (!this || !gameObject.activeInHierarchy) yield break;
            if (col)
            {
                var info = new DamageInfo
                {
                    amount = tickDamage,
                    type = type,
                    critMult = 1f,
                    source = source,
                    hitPoint = transform.position
                };
                Combat.ApplyHit(col, info);
            }
            yield return new WaitForSeconds(Mathf.Max(0.01f, interval));
        }
        // Auto-remove runner if no other coroutines are left
        if (this) Destroy(this);
    }
}
