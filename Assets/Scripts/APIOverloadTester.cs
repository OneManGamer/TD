using UnityEngine;
using TD.Combat;

public class APIOverloadTester : MonoBehaviour
{
    public GameObject enemy;
    public float baseDamage = 12f;
    public DamageType type = DamageType.Physical;

    void Update()
    {
        if (!enemy) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            var ctx = new HitContext(gameObject, enemy, type, enemy.transform.position, Vector3.up);
            bool hit = CombatIntegrationAPI.ApplyHit(gameObject, enemy, baseDamage, type, in ctx);
            Debug.Log($"[GO Overload] hit={hit}, damage={baseDamage} {type}");
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            var ctx = new HitContext(gameObject, enemy, type, enemy.transform.position, Vector3.up, isCrit:true, critMultiplier:2f);
            bool hit = CombatIntegrationAPI.ApplyHit(gameObject, enemy, baseDamage, type, in ctx);
            Debug.Log($"[GO Overload CRIT] hit={hit}, damage={baseDamage}*2 {type}");
        }
    }
}
