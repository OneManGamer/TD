using UnityEngine;
using TD.Combat;

public class QuickHitscanTester : MonoBehaviour
{
    public Transform target;
    public float range = 20f;
    public float damage = 10f;
    public DamageType damageType = DamageType.Physical;
    public LayerMask hitMask = ~0;

    void Update()
    {
        // Always face the target if assigned
        if (target) transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized, Vector3.up);

        // Draw the ray so you can see it
        Debug.DrawRay(transform.position, transform.forward * range, Color.green);

        // Fire on top row 1, Space, or Mouse0
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                CombatIntegrationAPI.ApplyHit(gameObject, hit.collider, damage, damageType, hit.point, hit.normal);
                Debug.Log($"[Hitscan] Dealt {damage} {damageType} to {hit.collider.name} at {hit.point}");
            }
            else
            {
                Debug.Log("[Hitscan] No hit.");
            }
        }
    }
}
